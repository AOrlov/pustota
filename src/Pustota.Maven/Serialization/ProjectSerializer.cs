﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Pustota.Maven.Models;
using Pustota.Maven.Serialization.Data;

namespace Pustota.Maven.Serialization
{
	internal class ProjectSerializer : IProjectSerializerWithUpdate
	{
		private readonly IDataFactory _dataFactory;

		public ProjectSerializer(IDataFactory dataFactory)
		{
			_dataFactory = dataFactory;
		}

		public IProject Deserialize(string content)
		{
			var document = XDocument.Parse(content);
			var pom = new PomDocument(document);
			var project = _dataFactory.CreateProject();
			LoadProject(pom, project);
			return project;
		}

		public string Serialize(IProject project)
		{
			var pom = new PomDocument();
			SaveProject(project, pom);
			return pom.ToString();
		}

		public string Serialize(IProject project, string contentToUpdate)
		{
			var document = XDocument.Parse(contentToUpdate);
			var pom = new PomDocument(document);
			SaveProject(project, pom);
			return pom.ToString();
		}

		// REVIEW: element is not PomDocument, it is wrapper XElement (dependency, parent)
		internal void LoadProjectReference(PomElement element, IProjectReference projectReference)
		{
			projectReference.ArtifactId = element.ReadElementValueOrNull("artifactId");
			projectReference.GroupId = element.ReadElementValueOrNull("groupId");
			projectReference.Version = element.ReadElementValueOrNull("version").ToVersion();
		}

		// REVIEW: element is not PomDocument, it is wrapper XElement (dependency, parent)
		internal void SaveProjectReference(IProjectReference projectReference, PomElement element)
		{
			element.SetElementValue("groupId", projectReference.GroupId);
			element.SetElementValue("artifactId", projectReference.ArtifactId);
			element.SetElementValue("version", projectReference.Version.Value);
		}

		internal void LoadParentReference(PomElement rootElement, IProject project)
		{
			var parentElement = rootElement.SingleOrNull("parent");
			if (parentElement == null)
			{
				project.Parent = null;
			}
			else
			{
				var parentReference = _dataFactory.CreateParentReference();
				LoadProjectReference(parentElement, parentReference);
				parentReference.RelativePath = parentElement.ReadElementValueOrNull("relativePath");
				project.Parent = parentReference;
			}
		}

		internal void SaveParentReference(IProject project, PomElement rootElement)
		{
			IParentReference parentReference = project.Parent;
			if (parentReference == null)
			{
				rootElement.RemoveElement("parent");
			}
			else
			{
				var parentElement = rootElement.SingleOrCreate("parent");
				SaveProjectReference(parentReference, parentElement);
				parentElement.SetElementValue("relativePath", parentReference.RelativePath);
			}
		}

		public IProperty LoadProperty(PomElement element)
		{
			var property = _dataFactory.CreateProperty();
			property.Name = element.LocalName;
			property.Value = element.Value;
			return property;
		}

		public void SaveProperty(IProperty property, PomElement element)
		{
			element.SetElementValue(property.Name, property.Value);
		}


		internal IModule LoadModule(PomElement element)
		{
			var module = _dataFactory.CreateModule();
			module.Path = element.Value;
			return module;
		}

		public void SaveModule(IModule module, PomElement element)
		{
			element.Value = module.Path;
		}


		internal IDependency LoadDependency(PomElement element)
		{
			IDependency dependency = _dataFactory.CreateDependency();

			LoadProjectReference(element, dependency);

			dependency.Scope = element.ReadElementValueOrNull("scope");
			dependency.Type = element.ReadElementValueOrNull("type");
			dependency.Classifier = element.ReadElementValueOrNull("classifier");

			bool optional;
			dependency.Optional = bool.TryParse(element.ReadElementValueOrNull("optional"), out optional) && optional;

			dependency.Exclusions = new BlackBox(element.SingleOrNull("exclusions"));
	
			return dependency;
		}

		internal void SaveDependency(IDependency dependency, PomElement element)
		{
			SaveProjectReference(dependency, element);

			element.SetElementValue("type", dependency.Type);
			element.SetElementValue("classifier", dependency.Classifier);
			element.SetElementValue("scope", dependency.Scope);

			if (dependency.Optional)
				element.SetElementValue("optional", "true");

			if (!dependency.Exclusions.IsEmpty)
				element.Add(dependency.Exclusions.Value as PomElement);
		}


		internal IPlugin LoadPlugin(PomElement element)
		{
			var plugin = _dataFactory.CreatePlugin();
			LoadProjectReference(element, plugin);

			bool extensions;
			plugin.Extensions = bool.TryParse(element.ReadElementValueOrNull("extensions"), out extensions) && extensions;

			plugin.Executions = new BlackBox(element.SingleOrNull("executions"));
			plugin.Configuration = new BlackBox(element.SingleOrNull("configuration"));

			plugin.Dependencies = element
				.ReadElements("dependencies", "dependency")
				.Select(LoadDependency)
				.ToList();

			return plugin;
		}

		protected void SavePlugin(IPlugin plugin, PomElement element)
		{
			SaveProjectReference(plugin, element);

			if (plugin.Extensions)
				element.SetElementValue("extensions", "true");

			if (!plugin.Configuration.IsEmpty)
				element.Add(plugin.Configuration.Value as PomElement);

			if (!plugin.Executions.IsEmpty)
				element.Add(plugin.Executions.Value as PomElement);

			if (!plugin.Dependencies.Any())
			{
				element.RemoveElement("dependencies");
			}
			else
			{
				var dependenciesNode = element.SingleOrCreate("dependencies");
				dependenciesNode.RemoveAllChildElements();
				foreach (IDependency dependency in plugin.Dependencies)
				{
					var dependencyNode = dependenciesNode.AddElement("dependency");
					SaveDependency(dependency, dependencyNode);
				}
			}
		}


		internal void LoadBuildContainer(PomElement element, IBuildContainer container)
		{
			var propertiesElement = element.SingleOrNull("properties");
			if (propertiesElement != null)
			{
				container.Properties = propertiesElement.Elements().Select(LoadProperty).ToList();
			}

			container.Modules = element
				.ReadElements("modules", "module")
				.Select(LoadModule)
				.ToList();

			container.Dependencies = element
				.ReadElements("dependencies", "dependency")
				.Select(LoadDependency)
				.ToList();

			container.DependencyManagement = element
				.ReadElements("dependencyManagement", "dependencies", "dependency")
				.Select(LoadDependency)
				.ToList();

			container.Plugins = element.ReadElements("build", "plugins", "plugin")
				.Select(LoadPlugin).ToList();

			container.PluginManagement = element.ReadElements("build", "pluginManagement", "plugins", "plugin")
				.Select(LoadPlugin).ToList();

			container.TestResources = new BlackBox(element.SingleOrNull("build", "testResources"));

		}

		internal void SaveBuildContainer(IBuildContainer container, PomElement element)
		{
			if (!container.Properties.Any())
			{
				element.RemoveElement("properties");
			}
			else
			{
				var propertiesNode = element.SingleOrCreate("properties");
				propertiesNode.RemoveAllChildElements();
				foreach (IProperty property in container.Properties)
				{
					SaveProperty(property, propertiesNode);
				}
			}

			if (!container.Modules.Any())
			{
				element.RemoveElement("modules");
			}
			else
			{
				var modulesNode = element.SingleOrCreate("modules");
				modulesNode.RemoveAllChildElements();
				foreach (IModule module in container.Modules.Where(m => !string.IsNullOrEmpty(m.Path)))
				{
					var moduleNode = modulesNode.AddElement("module");
					SaveModule(module, moduleNode);
				}
			}

			if (!container.Dependencies.Any())
			{
				element.RemoveElement("dependencies");
			}
			else
			{
				var dependenciesNode = element.SingleOrCreate("dependencies");
				dependenciesNode.RemoveAllChildElements();
				foreach (IDependency dependency in container.Dependencies)
				{
					var dependencyNode = dependenciesNode.AddElement("dependency");
					SaveDependency(dependency, dependencyNode);
				}
			}

			if (!container.DependencyManagement.Any())
			{
				element.RemoveElement("dependencyManagement");
			}
			else
			{
				var dependencyManagementNode = element.SingleOrCreate("dependencyManagement");
				var dependenciesNode = dependencyManagementNode.SingleOrCreate("dependencies");
				dependenciesNode.RemoveAllChildElements();
				foreach (IDependency dependency in container.DependencyManagement)
				{
					var dependencyNode = dependenciesNode.AddElement("dependency");
					SaveDependency(dependency, dependencyNode);
				}
			}
			

			// empty build section 
			// REVIEW: need refactoring
			if (!container.Plugins.Any() && !container.PluginManagement.Any() && container.TestResources.IsEmpty) 
			{
				element.RemoveElement("build");
			}
			else
			{
				var buildNode = element.SingleOrCreate("build");
				if (!container.Plugins.Any())
				{
					buildNode.RemoveElement("plugins");
				}
				else
				{
					var pluginsNode = buildNode.SingleOrCreate("plugins");
					pluginsNode.RemoveAllChildElements();
					foreach (var plugin in container.Plugins)
					{
						var pluginNode = pluginsNode.AddElement("plugin");
						SavePlugin(plugin, pluginNode);
					}
				}

				if (!container.PluginManagement.Any())
				{
					buildNode.RemoveElement("pluginManagement");
				}
				else
				{
					var pluginManagementNode = buildNode.SingleOrCreate("pluginManagement");
					var pluginManagementPluginsNode = pluginManagementNode.SingleOrCreate("plugins");

					pluginManagementPluginsNode.RemoveAllChildElements();
					foreach (var plugin in container.PluginManagement)
					{
						var pluginNode = pluginManagementPluginsNode.AddElement("plugin");
						SavePlugin(plugin, pluginNode);
					}
				}

				if (container.TestResources.IsEmpty)
				{
					buildNode.RemoveElement("testResources");
				}
				else
				{
					var testResources = buildNode.SingleOrCreate("testResources");
					testResources.ReplaceWith(container.TestResources.Value as PomElement);
				}
			}
		}

		internal IProfile LoadProfile(PomElement element)
		{
			var profile = _dataFactory.CreateProfile();
			profile.Id = element.ReadElementValueOrNull("id");
			LoadBuildContainer(element, profile);
			return profile;
		}

		internal void SaveProfile(IProfile profile, PomElement element)
		{
			if (string.IsNullOrEmpty(profile.Id))
			{
				element.RemoveElement("id");
			}
			else
			{
				element.SetElementValue("id", profile.Id);
			}
			SaveBuildContainer(profile, element);
		}


		internal void LoadProject(PomDocument pom, IProject project)
		{
			var rootElement = pom.RootElement;

			LoadProjectReference(rootElement, project);
			LoadParentReference(rootElement, project);

			project.Packaging = rootElement.ReadElementValueOrNull("packaging");
			project.Name = rootElement.ReadElementValueOrNull("name");
			project.ModelVersion = rootElement.ReadElementValueOrNull("modelVersion");

			LoadBuildContainer(rootElement, project);

			project.Profiles = rootElement.ReadElements("profiles", "profile")
				.Select(LoadProfile).ToList();
		}

		internal void SaveProject(IProject project, PomDocument pom)
		{
			var rootElement = pom.RootElement;

			SaveProjectReference(project, rootElement);
			SaveParentReference(project, rootElement);

			rootElement.SetElementValue("packaging", project.Packaging);
			rootElement.SetElementValue("name", project.Name);
			rootElement.SetElementValue("modelVersion", project.ModelVersion);

			SaveBuildContainer(project, rootElement);

			if (!project.Profiles.Any())
			{
				rootElement.RemoveElement("profiles");
			}
			else
			{
				var profileNode = rootElement.SingleOrCreate("profiles");

				HashSet<PomElement> usedElements = new HashSet<PomElement>(new PomElement.Comparer());
				foreach (var profile in project.Profiles)
				{
					var profElement = profileNode
						.Elements()
						.FirstOrDefault(e => e.ReadElementValueOrNull("id") == profile.Id) ?? profileNode.AddElement("profile");

					usedElements.Add(profElement);
					SaveProfile(profile, profElement);
				}

				//delete all profiles elements which are not in the Profiles array
				foreach (var profElem in profileNode.Elements().Where(e => !usedElements.Contains(e)))
					profElem.Remove();
			}
		}

	}
}
