namespace Pustota.Maven.Models
{
	public interface IResolvedProjectData
	{
		string GroupId { get; }
		string Version { get; }
		bool? IsSnapshot { get; }
		string ParentPath { get; }
	}
}