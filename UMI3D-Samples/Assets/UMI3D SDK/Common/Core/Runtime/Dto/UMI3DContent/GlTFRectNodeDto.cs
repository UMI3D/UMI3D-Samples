/*
Copyright 2019 - 2021 Inetum

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections.Generic;

namespace umi3d.common
{
    /// <summary>
    /// DTO to describe a rect node in the glTF scene graph.
    /// </summary>
    [System.Serializable]
    public class GlTFRectNodeDto : GlTFNodeDto
    {
        /// <summary>
        /// Size of the rectangle relative to the distance between the two anchors.
        /// </summary>
        public Vector2Dto sizeDelta { get; set; }

        /// <summary>
        /// The upper right corner's anchor point as a fraction relative to 
        /// the size of the parent rectangle. 
        /// </summary>
        /// (0,0) corresponds to an anchor at the lower left corner of the parent,
        /// while (1,1) corresponds to an anchor at the parent's upper right corner.
        public Vector2Dto anchorMax { get; set; }

        /// <summary>
        /// The lower left corner's anchor point as a fraction relative to 
        /// the size of the parent rectangle. 
        /// </summary>
        /// (0,0) corresponds to an anchor at the e lower left corner of the parent, 
        /// while (1,1) corresponds to an anchor at the parent's upper right corne.
        public Vector2Dto anchorMin { get; set; }

        /// <summary>
        /// Position of the pivot point as a fraction relative to 
        /// the size of the rectangle.
        /// </summary>
        /// (0,0) corresponds to the lower left corner 
        /// while (1,1) corresponds to the upper right corner.
        public Vector2Dto pivot { get; set; }
    }
}
