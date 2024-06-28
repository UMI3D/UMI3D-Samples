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

using inetum.unityUtils;
using System.Collections.Generic;
using System.Threading.Tasks;
using umi3d.common;
using umi3d.common.interaction;
using umi3d.common.interaction.form;
using umi3d.common.interaction.form.ugui;
using umi3d.edk;

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

        public virtual async Task<common.interaction.ConnectionFormDto> GenerateForm(User user)
        {
            var form = new common.interaction.ConnectionFormDto()
            {
                globalToken = user.globalToken,
                name = "Connection",
                description = null,
                fields = new List<AbstractParameterDto>()
            };

            form.fields.Add(
                new StringParameterDto()
                {
                    id = 1,
                    name = "Login",
                    value = ""
                });
            form.fields.Add(
                new StringParameterDto()
                {
                    id = 2,
                    privateParameter = true,
                    name = "Password",
                    value = ""
                });
            form.fields.Add(
                new EnumParameterDto<string>()
                {
                    id = 3,
                    name = "Select an option",
                    possibleValues = new List<string>() { "ValueA", "ValueB", "ValueC", "ValueD", "Une Pomme", "@&&&é" },
                    value = ""
                });


            return await Task.FromResult(form);
        }

        public virtual async Task<umi3d.common.interaction.form.ConnectionFormDto> GenerateDivForm(User user)
        {
            ulong nextId = 0;
            var form = new umi3d.common.interaction.form.ConnectionFormDto() {
                globalToken = user.globalToken,
                id = nextId++,
                name = "Login",
                FirstChildren = new List<DivDto>() {
                    new PageDto() {
                        name = "Login",
                        id = nextId++,
                        FirstChildren = new List<DivDto>() {
                            new InputDto<string>() {
                                Name = "Username",
                                TextType = TextType.Text,
                                PlaceHolder = "John Doe",
                                id = nextId++,
                                styles = new List<StyleDto>() {
                                    new StyleDto() {
                                        variants = new List<VariantStyleDto> () {
                                            new UGUIStyleVariantDto() {
                                                StyleVariantItems = new List<UGUIStyleItemDto>() {
                                                    new PositionStyleDto() {
                                                        posX = 0,
                                                        posY = 78,
                                                        posZ = 0,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new InputDto<string>() {
                                Name = "Password",
                                TextType = TextType.Password,
                                PlaceHolder = "***************",
                                id = nextId++,
                                styles = new List<StyleDto>() {
                                    new StyleDto() {
                                        variants = new List<VariantStyleDto> () {
                                            new UGUIStyleVariantDto() {
                                                StyleVariantItems = new List<UGUIStyleItemDto>() {
                                                    new PositionStyleDto() {
                                                        posX = 0,
                                                        posY = -35,
                                                        posZ = 0,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                    new PageDto() {
                        name = "Pin",
                        id = nextId++,
                        FirstChildren = new List<DivDto>() {
                            new InputDto<string>() {
                                Name = "Pin",
                                TextType = TextType.Number,
                                PlaceHolder = "123456",
                                id = nextId++,
                                styles = new List<StyleDto>() {
                                    new StyleDto() {
                                        variants = new List<VariantStyleDto> () {
                                            new UGUIStyleVariantDto() {
                                                StyleVariantItems = new List<UGUIStyleItemDto>() {
                                                    new PositionStyleDto() {
                                                        posX = 0,
                                                        posY = 28,
                                                        posZ = 0,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                    new ButtonDto() {
                        Name = "OK",
                        Text = "OK",
                        buttonType = ButtonType.Submit,
                        resource = new ResourceDto() {
                            variants = new List<FileDto>() {
                                new FileDto() {
                                    url = UMI3DServer.publicRepository + "/button ok.png",
                                    format = "png",
                                    metrics = new() {
                                        resolution = 8,
                                        size = 0.6f,
                                    }
                                }
                            }
                        },
                        id = nextId++,
                        styles = new List<StyleDto>() {
                            new StyleDto() {
                                variants = new List<VariantStyleDto> () {
                                    new UGUIStyleVariantDto() {
                                        StyleVariantItems = new List<UGUIStyleItemDto>() {
                                            new PositionStyleDto() {
                                                posX = 0,
                                                posY = -151,
                                                posZ = 0,
                                            },
                                            new SizeStyleDto() {
                                                width = 95,
                                                height = 64
                                            },
                                            new ColorStyleDto() {
                                                color = new ColorDto() {
                                                    R = 0.447f,
                                                    G = 0.447f,
                                                    B = 0.447f,
                                                    A = 1,
                                                }
                                            },
                                            new TextStyleDto() {
                                                fontSize = 24,
                                                color = new ColorStyleDto() {
                                                    color = new ColorDto() {
                                                        R = 1,
                                                        G = 1,
                                                        B = 1,
                                                        A = 1,
                                                    }
                                                },
                                                fontAlignments = new List<E_FontAlignment>() {
                                                    E_FontAlignment.Center
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new ButtonDto() {
                        Name = "< BACK",
                        Text = "< BACK",
                        buttonType = ButtonType.Back,
                        resource = new ResourceDto() {
                            variants = new List<FileDto>() {
                                new FileDto() {
                                    url = UMI3DServer.publicRepository + "/button ok.png",
                                    format = "png",
                                    metrics = new() {
                                        resolution = 8,
                                        size = 0.6f,
                                    }
                                }
                            }
                        },
                        id = nextId++,
                        styles = new List<StyleDto>() {
                            new StyleDto() {
                                variants = new List<VariantStyleDto> () {
                                    new UGUIStyleVariantDto() {
                                        StyleVariantItems = new List<UGUIStyleItemDto>() {
                                            new PositionStyleDto() {
                                                posX = -408,
                                                posY = 220,
                                                posZ = 0,
                                            },
                                            new SizeStyleDto() {
                                                width = 100,
                                                height = 32
                                            },
                                            new ColorStyleDto() {
                                                color = new ColorDto() {
                                                    R = 0.447f,
                                                    G = 0.447f,
                                                    B = 0.447f,
                                                    A = 1,
                                                }
                                            },
                                            new TextStyleDto() {
                                                fontSize = 18,
                                                color = new ColorStyleDto() {
                                                    color = new ColorDto() {
                                                        R = 1,
                                                        G = 1,
                                                        B = 1,
                                                        A = 1,
                                                    }
                                                },
                                                fontAlignments = new List<E_FontAlignment>() {
                                                    E_FontAlignment.Center
                                                }
                                            }
                                        }
                                    }
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

        public virtual async Task<List<AssetLibraryDto>> GetLibraries(User user)
        {
            return await Task.FromResult<List<AssetLibraryDto>>(null);
        }

        public virtual async Task<bool> isFormValid(User user, common.interaction.FormAnswerDto formAnswer)
        {
            UnityEngine.Debug.Log(formAnswer.ToJson(Newtonsoft.Json.TypeNameHandling.None));

            SetToken(user);
            return await Task.FromResult(true);
        }

        public virtual async Task<bool> isDivFormValid(User user, common.interaction.form.FormAnswerDto formAnswer)
        {
            UnityEngine.Debug.Log(formAnswer.ToJson(Newtonsoft.Json.TypeNameHandling.Auto));

            SetToken(user);
            if (formAnswer.isCancelation || formAnswer.isBack)
                return await Task.FromResult(false);
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