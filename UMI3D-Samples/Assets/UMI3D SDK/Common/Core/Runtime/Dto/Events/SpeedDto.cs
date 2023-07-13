﻿/*
Copyright 2019 - 2023 Inetum

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

namespace umi3d.common
{
    /// <summary>
    /// Dto to state movement speed;
    /// </summary>
    public class SpeedDto
    {
        /// <summary>
        /// Speed when going forward
        /// </summary>
        public float forwardSpeed { get; set; }

        /// <summary>
        /// Speed when going Backward
        /// </summary>
        public float backwardSpeed { get; set; }

        /// <summary>
        /// speed when going side way
        /// </summary>
        public float sideSpeed { get; set; }
    }
}