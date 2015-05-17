using System.Collections.Generic;
using RegulatedNoise.Core.DomainModel;

namespace RegulatedNoise.Core
{
	public interface ICommodities: ICollection<Commodity>
	{
		string GetBasename(string commodityName);
		
		void Update(Commodity commodity);

		void Save(string filepath, bool backupPrevious);
	}
}