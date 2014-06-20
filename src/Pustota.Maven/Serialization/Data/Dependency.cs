﻿using System.ComponentModel;
using Pustota.Maven.Models;

namespace Pustota.Maven.Serialization.Data
{
	[TypeConverter(typeof(ExpandableObjectConverter))]
	internal class Dependency : 
		ProjectReference,
		IDependency
	{
		public string Classifier { get; set; }
		public string Type { get; set; }
		public string Scope { get; set; }
		public bool Optional { get; set; }
	}
}
