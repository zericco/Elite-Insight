using System;
using System.Diagnostics;
using Elite.Insight.Core.Messaging;

namespace Elite.Insight.Core.DomainModel
{
	public class DataModel
	{
		public event EventHandler<ValidationEventArgs> OnValidationEvent;

		public event EventHandler<MarketDataEventArgs> OnMarketDataUpdate
		{
			add { GalacticMarket.OnMarketDataUpdate += value; }
			remove { GalacticMarket.OnMarketDataUpdate -= value; }
		}

		private readonly ILocalizer _localizer;
		private readonly IValidator<MarketDataRow> _marketDataValidator;

		private Lazy<Commodities> _commodities;
		public Commodities Commodities
		{
			get
			{
				return _commodities.Value;
			}
		}

		private readonly Lazy<GalacticMarket> _galacticMarket = new Lazy<GalacticMarket>();
		public GalacticMarket GalacticMarket
		{
			get
			{
				return _galacticMarket.Value;
			}
		}

		private StarMap _starMap;
		public StarMap StarMap
		{
			get
			{
				if (_starMap == null)
					_starMap = new StarMap();
				return _starMap;
			}
		}

		public DataModel(ILocalizer localizer, IValidator<MarketDataRow> marketDataValidator)
			:this(localizer, marketDataValidator, NopProgress.Instance)
		{
		}

		public DataModel(ILocalizer localizer, IValidator<MarketDataRow> marketDataValidator, IProgress<ProgressEvent> onInitializationProgress)
		{
			if (marketDataValidator == null)
			{
				throw new ArgumentNullException("marketDataValidator");
			}
			if (onInitializationProgress == null)
			{
				throw new ArgumentNullException("onInitializationProgress");
			}
			_localizer = localizer;
			_marketDataValidator = marketDataValidator;			
			_commodities = new Lazy<Commodities>(() => new Commodities(_localizer));
		}

		public void UpdateMarket(MarketDataRow marketdata)
		{
			PlausibilityState plausibility = Validate(marketdata);
			if (plausibility.Plausible)
			{
				marketdata.CommodityName = _localizer.TranslateInEnglish(marketdata.CommodityName);
				GalacticMarket.Update(marketdata);
			}
			else
			{
				RaiseValidationEvent(new ValidationEventArgs(plausibility));
			}
		}

		public PlausibilityState Validate(MarketDataRow marketdata)
		{
			return _marketDataValidator.Validate(marketdata);
		}

		protected virtual void RaiseValidationEvent(ValidationEventArgs e)
		{
			var handler = OnValidationEvent;
			if (handler != null)
				try
				{
					handler(this, e);
				}
				catch (Exception ex)
				{
					Trace.TraceError("validation notification failure: " + ex);
				}
		}
	}
}