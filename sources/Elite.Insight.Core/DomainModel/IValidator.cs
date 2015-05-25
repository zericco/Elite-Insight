namespace Elite.Insight.Core.DomainModel
{
	public interface IValidator<in TEntity>
	{
		PlausibilityState Validate(TEntity entity);
	}
}