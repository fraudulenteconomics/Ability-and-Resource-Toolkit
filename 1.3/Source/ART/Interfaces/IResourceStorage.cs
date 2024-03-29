﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ART
{
	public interface IResourceStorage
	{
        Dictionary<int, ResourceStorage> ResourceStorages { get; }
        HediffResource GetResourceFor(ResourceProperties resourceProperties);
    }
}