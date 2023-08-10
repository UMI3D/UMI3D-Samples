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
using UnityEngine;

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
            /*var form = new common.interaction.form.FormDto()
            {
                Name = "World",
                Pages = new List<PageDto> {
                    new PageDto()
                    {
                        Group = new GroupScrollViewDto()
                        {
                            Mode = UnityEngine.UIElements.ScrollViewMode.Horizontal,
                            Children = new List<DivDto>
                            {
                                new GroupDto()
                                {
                                    SubmitOnValidate = true,
                                    Children = new List<DivDto>
                                    {
                                        new LabelDto()
                                        {
                                            Text = "Test"
                                        },
                                        new ButtonDto()
                                        {
                                            Label = "G",
                                        }
                                    }
                                },
                                new GroupDto()
                                {
                                    SubmitOnValidate = true,
                                    Children = new List<DivDto>
                                    {
                                        new LabelDto()
                                        {
                                            Text = "Test"
                                        },
                                        new ButtonDto()
                                        {
                                            Label = "G",
                                        }
                                    }
                                },
                                new GroupDto()
                                {
                                    SubmitOnValidate = true,
                                    Children = new List<DivDto>
                                    {
                                        new LabelDto()
                                        {
                                            Text = "Test"
                                        },
                                        new ButtonDto()
                                        {
                                            Label = "G",
                                        }
                                    }
                                },
                                new GroupDto()
                                {
                                    SubmitOnValidate = true,
                                    Children = new List<DivDto>
                                    {
                                        new LabelDto()
                                        {
                                            Text = "Test"
                                        },
                                        new ButtonDto()
                                        {
                                            Label = "G",
                                        }
                                    }
                                },
                                new GroupDto()
                                {
                                    SubmitOnValidate = true,
                                    Children = new List<DivDto>
                                    {
                                        new LabelDto()
                                        {
                                            Text = "Test"
                                        },
                                        new ButtonDto()
                                        {
                                            Label = "G",
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            };*/

            
            var form = new common.interaction.form.FormDto()
            {
                Name = "Login",
                Description = "",
                Pages = new List<PageDto>()
                {
                    new PageDto()
                    {
                        Name = "Mail",
                        Group = new GroupDto()
                        {
                            CanRemember = true,
                            Children = new List<DivDto>()
                            {
                                new TextDto()
                                {
                                    Label = "Mail",
                                    PlaceHolder = "example@inetum.com",
                                    Type = TextType.Mail,
                                    Tooltip = "Enter your email adress"
                                },
                                new TextDto()
                                {
                                    Label = "Password",
                                    PlaceHolder = "password",
                                    Type = TextType.Password,
                                    Tooltip = "Enter your password"
                                },
                                new ButtonDto()
                                {
                                    Label = "< Back",
                                    Type = ButtonType.Back
                                },
                                new ButtonDto()
                                {
                                    Label = "Ok",
                                    Type = ButtonType.Submit
                                }
                            }
                        }
                    },
                    new PageDto()
                    {
                        Name = "Pin",
                        Group = new GroupDto()
                        {
                            CanRemember = true,
                            Children = new List<DivDto> { 
                                new TextDto()
                                {
                                    Label = "Pin",
                                    PlaceHolder = "123456",
                                    Type = TextType.Number
                                },
                                new ButtonDto()
                                {
                                    Label = "< Back",
                                    Type = ButtonType.Back
                                },
                                new ButtonDto()
                                {
                                    Label = "Ok",
                                    Type = ButtonType.Submit
                                }
                            }
                        }
                    }
                }
            };
            

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