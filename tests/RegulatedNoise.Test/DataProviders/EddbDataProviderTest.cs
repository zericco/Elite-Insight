using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.DataProviders.Eddb;

namespace RegulatedNoise.Test.DataProviders
{
	[TestClass]
	public class EddbDataProviderTest
	{
		[TestMethod]
		public void i_can_import_data()
		{
			var eddb = new EddbDataProvider();
			var model = new DataModel(new TestLocalizer(), new TestValidator());
			eddb.ImportData(model);
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