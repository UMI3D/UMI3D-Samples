using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using umi3d.common;
using UnityEditor;
using System.IO.Compression;
using System.Linq;

[ScriptedImporter(1, "umi3d")]
public class Umi3dImporter : ScriptedImporter
{
    public string id;
    public string umi3d_name;
    public string version;
    public string umi3dVersion;
    public string type;

    public string scene;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        if (Directory.Exists(Application.dataPath + "/../mod"))
            Directory.Delete(Application.dataPath + "/../mod", true);
        Directory.CreateDirectory(Application.dataPath + "/../mod");

        UnZip(new FileInfo(ctx.assetPath), Application.dataPath + "/../mod");

        Load(ctx);

        Directory.Delete(Application.dataPath + "/../mod", true);
    }

    #region Loading

    public void Load(AssetImportContext ctx)
    {
        LoadDlls();

        LoadBase();

        LoadScene(ctx);
    }

    public void LoadDlls()
    {
        if (Directory.Exists(Application.dataPath + "/../mod/contents/dll"))
        {
            if (!Directory.Exists(Application.dataPath + "/Mods"))
                Directory.CreateDirectory(Application.dataPath + "/Mods");

            foreach (string dllFile in Directory.GetFiles(Application.dataPath + "/../mod/contents/dll"))
            {
                File.Copy(dllFile, Application.dataPath + "/Mods/" + System.IO.Path.GetFileName(dllFile), true);
                System.Reflection.Assembly.LoadFile(Application.dataPath + "/Mods/" + System.IO.Path.GetFileName(dllFile));
            }

            AssetDatabase.Refresh();
        }
    }

    public void LoadBase()
    {
        if (File.Exists(Application.dataPath + "/../mod/info.json"))
        {
            string json = File.ReadAllText(Application.dataPath + "/../mod/info.json");

            UMI3DInfo info = UMI3DDtoSerializer.FromJson(json) as UMI3DInfo;

            id = info.id;
            umi3d_name = info.name;
            version = info.version;
            umi3dVersion = info.umi3dVersion;
            type = info.type;
        }
    }

    public void LoadScene(AssetImportContext ctx)
    {
        if (File.Exists(Application.dataPath + "/../mod/contents/jsons/scene.json"))
        {
            scene = File.ReadAllText(Application.dataPath + "/../mod/contents/jsons/scene.json");

            /*
            SaveReference references = new SaveReference();
            UMI3DDto dto = UMI3DDtoSerializer.FromJson(json);

            if (dto is GlTFEnvironmentDto environmentDto)
            {
                GameObject env = new GameObject("UMI3DEnvironment");
                SceneSaver.LoadEnvironment(environmentDto, env, references);

                ctx.AddObjectToAsset("scene", env);
                ctx.SetMainObject(env);
            }
            */
        }
    }

    #endregion

    #region Utility

    protected void UnZip(FileInfo source, string unzipPath)
    {
        if (!Directory.Exists(unzipPath))
            Directory.CreateDirectory(unzipPath);

        using (ZipArchive archive = ZipFile.OpenRead(source.FullName))
        {
            byte[] bytes = new byte[10240];
            int numbytes;

            foreach (ZipArchiveEntry e in archive.Entries)
            {
                string filePath = e.FullName;
                string fileFullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(unzipPath, e.FullName));
                // get the file attributes for file or directory
                string ext = System.IO.Path.GetExtension(fileFullPath);

                if (ext == null || ext.Length == 0)
                {
                    Directory.CreateDirectory(fileFullPath);
                }
                else
                {
                    DirectoryInfo di = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileFullPath));

                    using (FileStream fileStream = File.Create(fileFullPath))
                    using (var writer = new BinaryWriter(fileStream))
                    using (Stream zipEntryStream = e.Open())
                    {
                        while ((numbytes = zipEntryStream.Read(bytes, 0, 10240)) > 0)
                        {
                            writer.Write(bytes, 0, numbytes);
                        }
                    }
                }
            }
        }
    }

    #endregion
}