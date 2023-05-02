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

using umi3d.common;

namespace umi3d.edk
{
    /// <summary>
    /// Operation the enable/disable the bindings computations
    /// </summary>
    public class UpdateBindingsActivation : Operation
    {
        public UpdateBindingsActivation(bool areBindingsActivated)
        {
            this.areBindingsActivated = areBindingsActivated;
        }

        public bool areBindingsActivated;

        public override Bytable ToBytable(UMI3DUser user)
        {
            return UMI3DSerializer.Write(UMI3DOperationKeys.UpdateBindingsActivation)
                    + UMI3DSerializer.Write(ToOperationDto(user) as UpdateBindingsActivationDto);
        }

        public override AbstractOperationDto ToOperationDto(UMI3DUser user)
        {
            return new UpdateBindingsActivationDto() { areBindingsActivated = areBindingsActivated };
        }
    }
}