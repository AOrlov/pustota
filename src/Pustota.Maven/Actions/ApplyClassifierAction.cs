using System.Linq;
using Pustota.Maven.Models;

namespace Pustota.Maven.Actions
{
	public class ApplyClassifierAction
	{
		private readonly IProjectsRepository _projects;
		private readonly string _classifierName;
		private readonly string _classifierValue;

		public ApplyClassifierAction(IProjectsRepository projects, string classifierName, string classifierValue)
		{
			_projects = projects;
			_classifierName = classifierName;
			_classifierValue = classifierValue;
		}

		internal static string WrapProperty(string propertyName)
		{
			return "${" + propertyName + "}";
		}

		public void Execute()
		{
			string classifier = WrapProperty(_classifierName);

			foreach (var dependency in _projects.AllProjects.SelectMany(p => p.Operations().AllDependencies).Where(d => !string.IsNullOrEmpty(d.Classifier) && d.Classifier.Contains(classifier)))
			{
				dependency.Classifier = dependency.Classifier.Replace(classifier, _classifierValue);
			}
		}
	}
}