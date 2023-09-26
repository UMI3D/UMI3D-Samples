/*
Copyright 2019 Gfi Informatique

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

using umi3d.edk;
using umi3d.edk.interaction;

using UnityEngine;

public class ParametersDisplay : MonoBehaviour
{
    public StringParameter stringParameter;
    public StringEnumParameter stringEnum;
    public FloatRangeParameter rangeParameter;
    public BooleanParameter booleanParameter;

    public UIText enumText;
    public UIText stringText;
    public UIText rangeText;
    public UIText boolText;

    private void Start()
    {
        enumText.Text.SetValue(stringEnum.value);
        stringText.Text.SetValue(stringParameter.value);
        rangeText.Text.SetValue(rangeParameter.value.ToString());
        boolText.Text.SetValue(booleanParameter.value.ToString());

        stringEnum.onChange.AddListener(EnumParameterChange);
        stringParameter.onChange.AddListener(StringParameterChange);
        booleanParameter.onChange.AddListener(BoolParameterChange);
        rangeParameter.onChange.AddListener(RangeParameterChange);
    }

    private void StringParameterChange(AbstractParameter.ParameterEventContent<string> content)
    {
        Transaction t = new(true);
        t.AddIfNotNull(stringText.Text.SetValue(content.value));
        t.Dispatch();
    }

    private void EnumParameterChange(AbstractParameter.ParameterEventContent<string> content)
    {
        Transaction t = new(true);
        t.AddIfNotNull(enumText.Text.SetValue(content.value));
        t.Dispatch();
    }

    private void BoolParameterChange(AbstractParameter.ParameterEventContent<bool> content)
    {
        Transaction t = new(true);
        t.AddIfNotNull(boolText.Text.SetValue(content.value.ToString()));
        t.Dispatch();
    }

    private void RangeParameterChange(AbstractParameter.ParameterEventContent<float> content)
    {
        Transaction t = new(true);
        t.AddIfNotNull(rangeText.Text.SetValue(content.value.ToString()));
        t.Dispatch();
    }
}