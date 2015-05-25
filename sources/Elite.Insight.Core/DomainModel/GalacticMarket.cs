using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Elite.Insight.Annotations;
using Elite.Insight.Core.Helpers;

namespace Elite.Insight.Core.DomainModel
{
	public class GalacticMarket : IReadOnlyCollection<MarketDataRow>
	{
		public event EventHandler<MarketDataEventArgs> OnMarketDataUpdate
		{
			add { _allMarketDatas.OnMarketDataUpdate += value; }
			remove { _allMarketDatas.OnMarketDataUpdate -= value; }
		}

		public int Count { get { return _allMarketDatas.Count; } }

		public IEnumerable<string> StationIds
		{
			get
			{
				lock (_updating)
				{
					return _byStation.Select(s => s.StationID);
				}
			}
		}

		public IEnumerable<string> CommodityNames
		{
			get
			{
				lock (_updating)
				{
					return _byCommodity.Select(s => s.Commodity);
				}
			}
		}

		public IEnumerable<string> StationNames
		{
			get
			{
				lock (_updating)
				{
					return _byStation.Select(s => s.StationName);
				}
			}
		}

		public IEnumerable<string> Systems
		{
			get
			{
				lock (_updating)
				{
					return _byStation.Select(s => s.System);
				}
			}
		}

		public MarketDataRow this[string marketDataId]
		{
			get { return _allMarketDatas[marketDataId]; }
		}

		private readonly StationMarketCollection _byStation;

		private readonly CommodityMarketCollection _byCommodity;

		private readonly MarketDataCollection _allMarketDatas;

		private readonly object _updating = new object();

		public GalacticMarket()
		{
			_byStation = new StationMarketCollection();
			_byCommodity = new CommodityMarketCollection();
			_allMarketDatas = new MarketDataCollection() { EnableNotification = true };
		}

		public IEnumerable<MarketDataRow> StationMarket(string stationId)
		{
			return GetMarketDatas(stationId, _byStation);
		}

		public IEnumerable<MarketDataRow> CommodityMarket(string commodityName)
		{
			return GetMarketDatas(commodityName, _byCommodity);
		}

		protected IEnumerable<MarketDataRow> GetMarketDatas<TMarket>(string marketId, MarketCollection<TMarket> marketCollection)
			 where TMarket : Market
		{
			TMarket market;
			if (!marketCollection.TryGetValue(marketId, out market))
			{
				return new MarketDataRow[0];
			}
			else
			{
				return market;
			}
		}

		public bool Contains(MarketDataRow marketData)
		{
			return _allMarketDatas.Contains(marketData);
		}

		public bool Remove([NotNull] MarketDataRow marketDataRow)
		{
			lock (_updating)
			{
				bool deleted = _allMarketDatas.NotifiedRemove(marketDataRow);
				if (deleted)
				{
					_byStation.Remove(marketDataRow);
					_byCommodity.Remove(marketDataRow);
				}
				return deleted;
			}
		}

		public void RemoveAll(Predicate<MarketDataRow> filter)
		{
			lock (_updating)
			{
				_allMarketDatas.RemoveAll(filter);
				_byStation.RemoveAll(filter);
				_byCommodity.RemoveAll(filter);
			}
		}

		public void Clear()
		{
			lock (_updating)
			{
				_allMarketDatas.Clear();
				_byStation.Clear();
				_byCommodity.Clear();
			}
		}

		public Market.UpdateState Update([NotNull] MarketDataRow marketDataRow)
		{
			Market.UpdateState update;
			lock (_updating)
			{
				update = _allMarketDatas.Update(marketDataRow);
				switch (update)
				{
					case Market.UpdateState.Added:
						_byStation.Add(marketDataRow);
						_byCommodity.Add(marketDataRow);
						break;
					case Market.UpdateState.Replace:
						_byStation[marketDataRow.StationID].Set(marketDataRow);
						_byCommodity[marketDataRow.CommodityName].Set(marketDataRow);
						break;
					case Market.UpdateState.Discarded:
						break;
					default:
						throw new ArgumentOutOfRangeException(update + " unhandled update operation");
				}
			}
			return update;
		}

		public void Import([NotNull] IEnumerable<MarketDataRow> marketDataRows)
		{
			if (marketDataRows == null) throw new ArgumentNullException("marketDataRows");
			lock (_updating)
			{
				foreach (MarketDataRow marketDataRow in marketDataRows)
				{
					_allMarketDatas.Add(marketDataRow);
					_byStation.Add(marketDataRow);
					_byCommodity.Add(marketDataRow);
				}
			}
		}

		public void Import([NotNull] MarketDataRow marketDataRow)
		{
			if (marketDataRow == null) throw new ArgumentNullException("marketDataRow");
			lock (_updating)
			{
				_allMarketDatas.Add(marketDataRow);
				_byStation.Add(marketDataRow);
				_byCommodity.Add(marketDataRow);
			}
		}

		public IEnumerator<MarketDataRow> GetEnumerator()
		{
			return _allMarketDatas.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerable<MarketDataRow> FindMarketData(string text)
		{
			return this.LevenFilter(text, md => md.MarketDataId);
		}

		protected class MarketDataCollection : Market
		{
			protected override string GetKeyForItem(MarketDataRow item)
			{
				return item.MarketDataId;
			}
		}

		protected abstract class MarketCollection<TMarket> : KeyedCollection<string, TMarket>
			 where TMarket : Market
		{
			public void Add([NotNull] MarketDataRow marketDataRow)
			{
				if (marketDataRow == null) throw new ArgumentNullException("marketDataRow");
				TMarket market;
				if (Dictionary == null || !Dictionary.TryGetValue(GetKey(marketDataRow), out market))
				{
					market = NewMarket(marketDataRow);
					Add(market);
				}
				market.Add(marketDataRow);
			}

			protected abstract string GetKey(MarketDataRow marketDataRow);

			public bool Remove([NotNull] MarketDataRow marketDataRow)
			{
				if (marketDataRow == null) throw new ArgumentNullException("marketDataRow");
				TMarket market;
				if (Dictionary == null || !Dictionary.TryGetValue(GetKey(marketDataRow), out market))
				{
					return false;
				}
				else
				{
					return market.Remove(marketDataRow);
				}
			}

			protected abstract TMarket NewMarket(MarketDataRow marketDataRow);

			public void AddRange([NotNull] IEnumerable<MarketDataRow> marketDataRows)
			{
				if (marketDataRows == null) throw new ArgumentNullException("marketDataRows");
				foreach (MarketDataRow marketDataRow in marketDataRows)
				{
					Add(marketDataRow);
				}
			}

			public bool TryGetValue(string marketId, out TMarket market)
			{
				if (Dictionary == null)
				{
					market = default(TMarket);
					return false;
				}
				else
				{
					return Dictionary.TryGetValue(marketId, out market);
				}
			}

			public void RemoveAll(Predicate<MarketDataRow> filter)
			{
				foreach (TMarket market in this)
				{
					market.RemoveAll(filter);
				}
			}
		}

		protected class StationMarketCollection : MarketCollection<StationMarket>
		{
			protected override string GetKeyForItem(StationMarket item)
			{
				return item.StationID;
			}

			protected override string GetKey(MarketDataRow marketDataRow)
			{
				return marketDataRow.StationID;
			}

			protected override StationMarket NewMarket(MarketDataRow marketDataRow)
			{
				return new StationMarket(marketDataRow.StationID, marketDataRow.SystemName, marketDataRow.StationName);
			}

			public override string ToString()
			{
				return "StationMarkets [" + Count + "]";
			}
		}

		protected class CommodityMarketCollection : MarketCollection<CommodityMarket>
		{
			protected override string GetKeyForItem(CommodityMarket item)
			{
				return item.Commodity;
			}

			protected override string GetKey(MarketDataRow marketDataRow)
			{
				return marketDataRow.CommodityName;
			}

			protected override CommodityMarket NewMarket(MarketDataRow marketDataRow)
			{
				return new CommodityMarket(marketDataRow.CommodityName);
			}

			public override string ToString()
			{
				return "CommodityMarkets [" + Count + "]";
			}
		}
	}

	public class MarketDataEventArgs : EventArgs
	{
		public readonly MarketDataRow Previous;

		public readonly MarketDataRow Actual;

		public bool IsAdded
		{
			get { return Previous == null; }
		}

		public bool IsRemoved
		{
			get { return Actual == null; }
		}

		public bool IsReplaced
		{
			get { return Previous != null && Actual != null; }
		}

		public MarketDataEventArgs(MarketDataRow previous = null, MarketDataRow actual = null)
		{
			Debug.Assert(previous != null || actual != null, "at least one marketdata should not be null");
			Previous = previous;
			Actual = actual;
		}
	}

	public class CommodityMarket : Market
	{
		public string Commodity { get; private set; }

		public CommodityMarket([NotNull] string commodityName)
		{
			if (commodityName == null) throw new ArgumentNullException("commodityName");
			Commodity = commodityName;
		}

		protected override string GetKeyForItem(MarketDataRow item)
		{
			return item.StationID;
		}

		public override string ToString()
		{
			return Commodity + " Market [" + Count + "]";
		}
	}

	public class StationMarket : Market
	{
		public string StationName { get; private set; }

		public string StationID { get; private set; }

		public string System { get; private set; }

		public StationMarket([NotNull] string stationId, string system, string stationName)
		{
			if (stationId == null) throw new ArgumentNullException("stationId");
			StationID = stationId;
			StationName = stationName;
			System = system;
		}

		protected override string GetKeyForItem(MarketDataRow item)
		{
			return item.CommodityName;
		}

		public override string ToString()
		{
			return StationName + " Market [" + Count + "]";
		}
	}
}
