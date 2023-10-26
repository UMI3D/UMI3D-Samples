﻿using System;
namespace umi3d.common
{
    /// <summary>
    /// Abstract DTO to describe a UMI3D entity.
    /// </summary>
    [Serializable]
    public class DistantEnvironmentDto : AbstractEntityDto, IEntity
    {
        public GlTFEnvironmentDto environmentDto { get; set; }
    }
}