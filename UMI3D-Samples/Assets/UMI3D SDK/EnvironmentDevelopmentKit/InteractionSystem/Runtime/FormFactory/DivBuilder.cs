/*
Copyright 2019 - 2024 Inetum

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
using umi3d.common.interaction.form.ugui;
using UnityEngine;

namespace umi3d.common.interaction.form
{
    public class DivBuilder<T> where T : DivDto, new()
    {
        protected T value;
        private UGUIStyleVariantDto style;
        private TextStyleDto textStyle;

        protected void InstantiateValue()
        {
            value = new T();
        }

        public DivBuilder<T> Position(float x, float y, float z)
        {
            CheckStyleCreated();
            style.StyleVariantItems.Add(new PositionStyleDto() { posX = x, posY = y, posZ = z });

            return this;
        }

        public DivBuilder<T> Position(float x, float y)
        {
            CheckStyleCreated();
            style.StyleVariantItems.Add(new PositionStyleDto() { posX = x, posY = y, posZ = 0 });

            return this;
        }

        public DivBuilder<T> Color(float r, float g, float b, float a)
        {
            CheckStyleCreated();
            style.StyleVariantItems.Add(new ColorStyleDto() { color = new ColorDto() { R = r, G = g, B = b, A = a } });

            return this;
        }

        public DivBuilder<T> Size(float width, float height)
        {
            CheckStyleCreated();
            style.StyleVariantItems.Add(new SizeStyleDto() { width = width, height = height });

            return this;
        }

        public DivBuilder<T> TextSize(float size)
        {
            CheckStyleCreated();
            CheckTextStyleCreated();
            textStyle.fontSize = size;

            return this;
        }

        public DivBuilder<T> TextColor(float r, float g, float b, float a)
        {
            CheckStyleCreated();
            CheckTextStyleCreated();
            textStyle.color = new ColorStyleDto() { color = new ColorDto() { R = r, G = g, B = b, A = a } };

            return this;
        }

        public DivBuilder<T> TextStyle(List<E_FontStyle> styles)
        {
            CheckStyleCreated();
            CheckTextStyleCreated();
            textStyle.fontStyles = styles;

            return this;
        }

        public DivBuilder<T> TextAlignement(List<E_FontAlignment> alignements)
        {
            CheckStyleCreated();
            CheckTextStyleCreated();
            textStyle.fontAlignments = alignements;

            return this;
        }

        public DivBuilder<PageDto> AddPage(string label)
        {
            var builder = new PageBuilder(label);
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        public LabelBuilder AddLabel(string label)
        {
            var builder = new LabelBuilder(label);
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        public ImageBuilder AddImage(string url, string format, AssetMetricDto metrics)
        {
            var builder = new ImageBuilder(url, format, metrics);
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        public ButtonBuilder AddButton(string label)
        {
            var builder = new ButtonBuilder(label);
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        public GroupBuilder AddGroup()
        {
            var builder = new GroupBuilder();
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        public InputBuilder<V> AddInput<V>(string label)
        {
            var builder = new InputBuilder<V>(label);
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        public RangeBuilder<V> AddRange<V>(V min, V max)
        {
            var builder = new RangeBuilder<V>(min, max);
            if (value.FirstChildren == null)
                value.FirstChildren = new List<DivDto>();
            value.FirstChildren.Add(builder.value);
            return builder;
        }

        protected static void SetIdRecursivly(DivDto div, ref ulong id)
        {
            div.id = id++;

            if (div.FirstChildren != null)
                foreach (var newDiv in div.FirstChildren)
                    SetIdRecursivly(newDiv, ref id);
        }

        private void CheckStyleCreated()
        {
            if (value.styles == null)
            {
                style = new UGUIStyleVariantDto() {
                    StyleVariantItems = new List<UGUIStyleItemDto>()
                };
                value.styles = new List<StyleDto>() {
                    new StyleDto() {
                        variants = new List<VariantStyleDto>() {
                            style
                        }
                    }
                };
            }
        }

        private void CheckTextStyleCreated()
        {
            if (textStyle == null)
            {
                textStyle = new TextStyleDto();
                style.StyleVariantItems.Add(textStyle);
            }
        }
    }
}