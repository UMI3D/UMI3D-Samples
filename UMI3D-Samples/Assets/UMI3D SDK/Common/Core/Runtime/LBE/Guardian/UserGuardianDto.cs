using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using umi3d.common.lbe;


namespace umi3d.common
{
    public class UserGuardianDto : AbstractBrowserRequestDto
    {
        /// <summary>
        /// Position anchor.
        /// </summary>
        public Vector3Dto position { get; set; }

        /// <summary>
        /// Rotation anchor.
        /// </summary>
        public Vector4Dto rotation { get; set; }

        /// <summary>
        /// Id anchor.
        /// </summary>
        public ulong trackableId { get; set; }

    }
}

