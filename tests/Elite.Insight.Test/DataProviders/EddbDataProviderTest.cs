using System;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.DataProviders.Eddb;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Elite.Insight.Test.DataProviders
{
	[TestClass]
	public class EddbDataProviderTest
	{
		[TestMethod]
		public void i_can_import_data()
		{
			var eddb = new EddbDataProvider();
			var model = new DataModel(new TestLocalizer(), new TestValidator());
			eddb.ImportData(model, true, ImportMode.Import);
		}
	}

	public class TestValidator : IValidator<MarketDataRow>
	{
		public PlausibilityState Validate(MarketDataRow entity)
		{
			throw new NotImplementedException();
		}
	}

	internal class TestLocalizer : ILocalizer
	{
		public string TranslateToCurrent(string toLocalize)
		{
			return toLocalize;
		}

		public string TranslateInEnglish(string toLocalize)
		{
			return toLocalize;
		}
	}
}