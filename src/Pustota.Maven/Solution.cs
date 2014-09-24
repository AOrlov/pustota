﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pustota.Maven.Models;
using Pustota.Maven.Serialization;
using Pustota.Maven.SystemServices;

namespace Pustota.Maven
{
	// TODO: support multiple repositories within solution
	internal class Solution : ISolution
	{
		private readonly IFileSystemAccess _fileIo;
		private readonly IProjectTreeLoader _loader;

		private IList<IProjectTreeItem> _projects;

		public string BaseDir { get; private set; }

		public IEnumerable<IProject> AllProjects { get { return _projects.Select(item => item.Project); } }
		public IEnumerable<IProjectTreeItem> Tree { get { return _projects; } }

		//public ProjectsValidations Validations { get; private set; }
		//public ExternalModulesRepository ExternalModules { get; private set; }

		internal Solution(IFileSystemAccess fileIo, IProjectTreeLoader loader)
		{
			_fileIo = fileIo;
			_loader = loader;
		}

		// REVIEW: loadDisconnectedProjects need to be rewired
		internal void Open(string fileOrFolderName, bool loadDisconnectedProjects)
		{
			if (fileOrFolderName == null) throw new ArgumentNullException("fileOrFolderName");

			string fullPath = _fileIo.GetFullPath(fileOrFolderName);
			string filePath;

			if (_fileIo.IsFileExist(fullPath))
			{
				BaseDir = _fileIo.GetDirectoryName(fullPath);
				filePath = fullPath;
			}
			else if (_fileIo.IsDirectoryExist(fullPath))
			{
				BaseDir = fullPath;
				filePath = _fileIo.Combine(fullPath, ProjectTreeLoader.ProjectFilePattern);
			}
			else
			{
				throw new FileNotFoundException("Solution entry point is missing", fullPath);
			}

			if (loadDisconnectedProjects)
			{
				_projects = _loader.ScanForProjects(BaseDir).ToList();
			}
			else
			{
				_projects = _loader.LoadProjectTree(filePath).ToList();
			}
		}

//		public IEnumerable<ValidationError> Validate()
//		{
//			Validations.Validate();
//			return Validations.ValidationErrors;
//		}

//		private void Load()
//		{
////			ExternalModules = new ExternalModulesRepository(BaseDir); // REVIEW: BaseDir should be solution 
////			Validations = new ProjectsValidations(ProjectsRepository, ExternalModules);
//		}

//		public bool Changed
//		{
//			// _ignoredValidations.Changed
//			// _externalModules.Changed
//			get { return ProjectsRepository.Changed; }
//		}

		public void ForceSaveAll()
		{
			_loader.SaveProjects(_projects);
		}

		//public bool Changed
		//{
		//	get { return AllProjectNodes.Any(node => node.Changed); }
		//}

		//public IEnumerable<ProjectNode> GetRootProjects()
		//{
		//	var pathesOfModules = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		//	foreach (var node in _allProjectNodes)
		//	{
		//		if (node.BaseDir == null) // REVIEW: what is the use case?
		//		{
		//			continue;
		//		}

		//		foreach (var modulePath in node.ModulePathList)
		//		{
		//			pathesOfModules.Add(modulePath);
		//		}
		//	}
		//	return _allProjectNodes
		//		.Where(node => !pathesOfModules.Contains(node.FullPath));
		//}

		//public IEnumerable<ProjectNode> GetProjectModules(ProjectNode projectNode)
		//{
		//	foreach (var modulePath in projectNode.ModulePathList)
		//	{
		//		var moduleNode = _allProjectNodes
		//			.SingleOrDefault(node => modulePath.Equals(node.FullPath, StringComparison.InvariantCultureIgnoreCase));
		//		if (moduleNode != null)
		//		{
		//			yield return moduleNode;
		//		}
		//	}
		//}

		//public bool IsItUsed(IProjectReference projectReference)
		//{
		//	var creteria = new SearchOptions
		//	{
		//		LookForParents = true,
		//		LookForDependent = true,
		//		LookForPlugin = true,
		//		OnlyDirectUsages = true,
		//		StrictVersion = true
		//	};

		//	return AllProjectNodes.Any(node => node.UsesProjectAs(projectReference, creteria));
		//}

		//public bool ContainsProject(IProjectReference projectReference, bool strictVersion)
		//{
		//	return AllProjectNodes.Any(p => p.ReferenceEqualTo(projectReference, strictVersion));
		//}

		//public IEnumerable<ProjectNode> SelectProjectNodes(IProjectReference projectReference, bool strictVersion = false)
		//{
		//	return AllProjectNodes.Where(p => p.ReferenceEqualTo(projectReference, strictVersion));
		//}


		//public IProject FindFirstProject(IProjectReference projectReference)
		//{
		//	return AllProjects.FirstOrDefault(p => p.ReferenceEqualTo(projectReference));
		//}

		//public void LoadOneProject(string path)
		//{
		//	AllProjects.Add(_loader.LoadProject(path));
		//}

		//public void SaveChangedProjects()
		//{
		//	foreach (Project prj in AllProjects.Where(prj => prj.Changed))
		//	{
		//		_loader.SaveProject(prj, prj.FullPath);
		//		prj.Changed = false;
		//	}

		//	// REVIEW: aggregate to solution

		//	//if (_externalModules.Changed)
		//	//	SaveExternalModules();
		//	//if (_ignoredValidations.Changed)
		//	//	SaveIgnoredValidations();
		//}

	}
}
