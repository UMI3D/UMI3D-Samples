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
using System.Threading.Tasks;
using umi3d.common;
using umi3d.common.interaction;
using umi3d.common.interaction.form;
using umi3d.edk;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.UIElements.ToolbarMenu;

namespace umi3d.worldController
{
    /// <summary>
    /// Identity and Access manager that runs on the server as a standalone.
    /// </summary>
    public class StandAloneIAM : IIAM
    {
        protected readonly IEnvironment environment;
        public StandAloneIAM(IEnvironment environment) { this.environment = environment; }

        private readonly List<string> tokens = new List<string>();

        public virtual async Task<common.interaction.form.FormDto> GenerateForm(User user)
        {

            var form = new common.interaction.form.FormDto()
            {
                Name = "World",
                Pages = new List<PageDto> {
                    new PageDto()
                    {
                        Group = new GroupScrollViewDto()
                        {
                            Mode = ScrollViewMode.Horizontal,
                            Children = new List<DivDto>(),
                            Styles = new List<StyleDto>()
                            {
                                new StyleDto()
                                {
                                    Variants = new ()
                                    {
                                        new SizeStyleDto ()
                                        {
                                            Width = new Length(711, LengthUnit.Pixel),
                                            Height = StyleKeyword.Auto,
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            for (int i = 0; i < 5; i++)
            {
                var group = new GroupDto()
                {
                    SubmitOnValidate = true,
                    Children = new List<DivDto>
                    {
                        new GroupDto()
                        {
                            Children = new List<DivDto>
                            {
                                new ImageDto()
                                {
                                    Resource = new ResourceDto()
                                    {
                                        variants = new List<FileDto>()
                                        {
                                            new FileDto()
                                            {
                                                format = "png",
                                                extension = ".png",
                                                url = System.Uri.EscapeUriString(inetum.unityUtils.Path.Combine(UMI3DServer.GetResourcesUrl(), UMI3DNetworkingKeys.files, "public/picture.png"))
                                            }
                                        }
                                    },
                                    Styles = new List<StyleDto>()
                                    {
                                        new StyleDto()
                                        {
                                            Variants = new ()
                                            {
                                                new SizeStyleDto ()
                                                {
                                                    Width = new Length(227, LengthUnit.Pixel),
                                                    Height = new Length(265, LengthUnit.Pixel),
                                                }
                                            }
                                        }
                                    }
                                },
                                new ButtonDto()
                                {

                                    Styles = new List<StyleDto>()
                                    {
                                        new StyleDto()
                                        {
                                            Variants = new ()
                                            {
                                                new SizeStyleDto ()
                                                {
                                                    Width = new Length(49, LengthUnit.Pixel),
                                                    Height = new Length(49, LengthUnit.Pixel),
                                                }
                                            }
                                        },
                                        new StyleDto()
                                        {
                                            Variants = new ()
                                            {
                                                new PositionStyleDto ()
                                                {
                                                    Position = Position.Absolute,
                                                    Top = StyleKeyword.Auto,
                                                    Bottom = new Length(8, LengthUnit.Pixel),
                                                    Left = StyleKeyword.Auto,
                                                    Right = new Length(8, LengthUnit.Pixel),
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new LabelDto()
                        {
                            Text = "Test"
                        }
                    }
                };
                form.Pages[0].Group.Children.Add(group);
            }


            /*var form = new common.interaction.form.FormDto()
            {
                Id = 1,
                Name = "Login",
                Description = "",
                Pages = new List<PageDto>()
                {
                    new PageDto()
                    {
                        Id = 11,
                        Name = "Mail",
                        Group = new GroupDto()
                        {
                            CanRemember = true,
                            Children = new List<DivDto>()
                            {
                                new TextDto()
                                {
                                    Id = 2,
                                    Label = "Mail",
                                    PlaceHolder = "example@inetum.com",
                                    Type = TextType.Mail,
                                    Tooltip = "Enter your email adress"
                                },
                                new TextDto()
                                {
                                    Id = 3,
                                    Label = "Password",
                                    PlaceHolder = "password",
                                    Type = TextType.Password,
                                    Tooltip = "Enter your password"
                                },
                                new ButtonDto()
                                {
                                    Id = 4,
                                    Label = "Ok",
                                    Type = ButtonType.Submit
                                }
                            }
                        }
                    },
                    new PageDto()
                    {
                        Id = 12,
                        Name = "Pin",
                        Group = new GroupDto()
                        {
                            Children = new List<DivDto> { 
                                new TextDto()
                                {
                                    Id = 5,
                                    Label = "Pin",
                                    PlaceHolder = "123456",
                                    Type = TextType.Number
                                },
                                new ButtonDto()
                                {
                                    Id = 6,
                                    Label = "Ok",
                                    Type = ButtonType.Submit
                                }
                            }
                        }
                    }
                }
            };*/
            

            return await Task.FromResult(form);
        }

        public virtual async Task<IEnvironment> GetEnvironment(User user)
        {
            return await Task.FromResult(environment);
        }

        public virtual async Task<List<LibrariesDto>> GetLibraries(User user)
        {
            return await Task.FromResult<List<LibrariesDto>>(null);
        }

        public virtual async Task<bool> isFormValid(User user, FormAnswerDto formAnswer)
        {
            UnityEngine.Debug.Log(formAnswer.ToJson(Newtonsoft.Json.TypeNameHandling.None));

            SetToken(user);
            return await Task.FromResult(true);
        }

        public virtual async Task<bool> IsUserValid(User user)
        {
            if (user.Token != null && tokens.Contains(user.Token))
                return await Task.FromResult(true);

            return await Task.FromResult(false);
        }

        public virtual async Task RenewCredential(User user)
        {
            SetToken(user);
            await Task.CompletedTask;
        }

        public void SetToken(User user)
        {
            if (user.Token == null || !tokens.Contains(user.Token))
            {
                string token = System.Guid.NewGuid().ToString();
                tokens.Add(token);
                user.Set(token);
            }
        }
    }
}