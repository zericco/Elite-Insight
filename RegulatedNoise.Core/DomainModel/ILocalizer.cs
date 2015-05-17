namespace RegulatedNoise.Core.DomainModel
{
	public interface ILocalizer
	{
		string TranslateToCurrent(string toLocalize);
		string TranslateInEnglish(string commodityName);
	}
}