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

using umi3d.common.userCapture.pose;

namespace umi3d.edk.userCapture.pose
{
    /// <summary>
    /// Pose condition true when one of the two contained condition is true.
    /// </summary>
    public class OrPoseCondition : IPoseAnimatorActivationCondition
    {
        public IPoseAnimatorActivationCondition conditionA;

        public IPoseAnimatorActivationCondition conditionB;

        public OrPoseCondition(IPoseAnimatorActivationCondition conditionA, IPoseAnimatorActivationCondition conditionB)
        {
            this.conditionA = conditionA;
            this.conditionB = conditionB;
        }

        public AbstractPoseConditionDto ToDto()
        {
            return new OrConditionDto()
            {
                ConditionA = conditionA.ToDto(),
                ConditionB = conditionB.ToDto()
            };
        }
    }
}