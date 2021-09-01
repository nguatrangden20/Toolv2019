using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SFB;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using MiniJSON;
using Libs;
using System.IO.Compression;
using System.Threading;


public class Manager : MonoBehaviour
{
    public TMP_InputField formResource;
    public TMP_InputField targetResource;
    public TMP_InputField fristAsset;

    public GameObject filePrefabs;
    public RawImage outputImage;
    public GameObject outputText;

    public TextMeshProUGUI fileSize;
    public TextMeshProUGUI pixel;

    Dictionary<string, GRPreloadType> preloadType = new Dictionary<string, GRPreloadType>();
    Dictionary<string, GRCacheType> cacheType = new Dictionary<string, GRCacheType>();
    List<GFileView> listFile = new List<GFileView>();

    public Slider progressBar;
    private bool checkEncryptClick;

    public void ShowExplorer(string path)
    {        
        switch (path)
        {
            case "Form Resource":
                var textFormResource = StandaloneFileBrowser.OpenFolderPanel("", "", false);
                if(textFormResource.Length > 0)
                    formResource.text = textFormResource[0];
                break;

            case "Target Resource":
                var textTargetResource = StandaloneFileBrowser.OpenFolderPanel("", "", false);
                if(textTargetResource.Length > 0)
                    targetResource.text = textTargetResource[0];
                break;

            case "Frist Asset":
                var textFristAsset = StandaloneFileBrowser.OpenFolderPanel("", "", false);
                if(textFristAsset.Length > 0)
                    fristAsset.text = textFristAsset[0];
                break;

            default: break;
        }
    }

    FileInfo[] fileInfos;
    public Toggle all;
    public Toggle change;
    public void Refresh()
    {
        checkEncryptClick = true;

        string pathMetaFolder = formResource.text + @"\" + "meta-folder";
        Directory.CreateDirectory(pathMetaFolder);
        var info = new DirectoryInfo(formResource.text);
        var metaFile = new DirectoryInfo(formResource.text + @"\" + "meta-folder").GetFiles();
        var listNameMetaFile = new List<string>();
        foreach (var item in metaFile)
        {
            listNameMetaFile.Add(item.Name);
        }
        if(change.isOn)
            fileInfos = info.GetFiles().Where(file => !listNameMetaFile.Contains(file.Name)).ToArray();
        else fileInfos = info.GetFiles();

        var parentObject = GameObject.Find("Canvas/BackGround/Mid/Left/Scroll View/Viewport/Content");
        if(parentObject.transform.childCount != 0)
            for (int i = 0; i < parentObject.transform.childCount; i++)
            {
                GameObject.Destroy(parentObject.transform.GetChild(i).gameObject);
            }
        
        foreach (var file in fileInfos)
        {
            GameObject go = Instantiate(this.filePrefabs, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            go.transform.SetParent(GameObject.Find("Canvas/BackGround/Mid/Left/Scroll View/Viewport/Content").transform, false);
            go.transform.localScale = new Vector3(1,1,1);

            go.gameObject.transform.Find("File Name/Text").GetComponent<TextMeshProUGUI>().text = file.Name;
            go.gameObject.transform.Find("Path/Text").GetComponent<TextMeshProUGUI>().text = file.FullName;  

            if(!preloadType.ContainsKey(file.Name)) preloadType.Add(file.Name, GRPreloadType.Queue);
            if(!cacheType.ContainsKey(file.Name)) cacheType.Add(file.Name, GRCacheType.Disk);

            AddListFile(file);
        }
    }

    public Toggle Queue;
    public Toggle Disk;
    public GameObject optional;
    private void SetOptional(bool onOff, string nameFile)
    {
        switch (preloadType[nameFile])
        {
            case GRPreloadType.OnDemand:
                optional.transform.Find("Preload/OnDemand").GetComponent<Toggle>().isOn = true;
                break;

            case GRPreloadType.Preload:
                optional.transform.Find("Preload/Preload").GetComponent<Toggle>().isOn = true;
                break;

            case GRPreloadType.Queue:
                optional.transform.Find("Preload/Queue").GetComponent<Toggle>().isOn = true;
                break;

            default: break;
        }

        switch (cacheType[nameFile])
        {
            case GRCacheType.Disk:
                optional.transform.Find("Cache/Disk").GetComponent<Toggle>().isOn = true;
                break;

            case GRCacheType.RamPersistent:
                optional.transform.Find("Cache/RamPersistent").GetComponent<Toggle>().isOn = true;
                break;

            case GRCacheType.None:
                optional.transform.Find("Cache/None").GetComponent<Toggle>().isOn = true;
                break;

            default: break;
        }

        optional.SetActive(onOff);
        staticNameFile = nameFile;
    }

    public void GetTexture(string path, string nameFile)
    {
        var bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(1,1);
        texture.LoadImage(bytes, false);

        OutputImage(texture);

        FileInfo file = new FileInfo(path);

        pixel.text = "Pixel: " + texture.width + "x" + texture.height;
        fileSize.text = "File Size: " + file.Length + " Bytes";

        pixel.gameObject.SetActive(true);
        fileSize.gameObject.SetActive(true);

        SetOptional(true, nameFile);
    }

    public void GetText(string path, string nameFile)
    {
        var text = File.ReadAllText(path);
        
        OutputText(text);

        FileInfo file = new FileInfo(path);

        fileSize.text = "File Size: " + file.Length + " Bytes";

        fileSize.gameObject.SetActive(true);
        pixel.gameObject.SetActive(false);

        SetOptional(true, nameFile);
    }


        private void OutputText(string text)
    {
        outputImage.gameObject.SetActive(false);
        outputText.gameObject.SetActive(true);
        
        outputText.transform.Find("Viewport/Content/Text").GetComponent<TextMeshProUGUI>().text = text;
    }

    private void OutputImage(Texture2D texture2D)
    {
        outputImage.gameObject.SetActive(true);
        outputText.gameObject.SetActive(false);

        outputImage.texture = texture2D;
    }

    public void EncryptButton()
    {
        Thread encrypt = new Thread(new ThreadStart(this.EncryptFile));
        if(checkEncryptClick) encrypt.Start();
    }

    float progressValue;
    private void EncryptFile()
    {
        checkEncryptClick = false;
        int count = 0;

        string pathMetaFolder = formResource.text + @"\" + "meta-folder";
        Directory.CreateDirectory(pathMetaFolder);        

        foreach (var file in fileInfos)
        {
            var assetsBytes = File.ReadAllBytes(file.FullName);

            byte [] encryptBytes = null;

            switch (CheckTypeFile(file))
            {
                case FileKind.Image:
                    encryptBytes = Encrypt(assetsBytes);
                    break;

                case FileKind.Text:
                    encryptBytes = Encrypt(assetsBytes, assetsBytes.Length);
                    break;
                
                default: break;
            }

            string encryptFilePath = targetResource.text + @"\" + file.Name;

            if(encryptBytes != null) WriteByte(encryptBytes, encryptFilePath);

            var metaFile = new GMetaFile()
            {
                Id = file.Name,
                Path = file.FullName,
                FileName = file.Name,
                FileSize = file.Length,
                Kind = CheckTypeFile(file),
                Date = file.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss")
            };

            string pathMetaFile = pathMetaFolder + @"\" + file.Name;
            WriteTextFile(metaFile, pathMetaFile);

            count++;
            progressValue = (count / fileInfos.Length) * 100 / 2;
            Debug.Log(progressValue);
        }

        DirectoryInfo zipfolder = new DirectoryInfo(targetResource.text);
        string nameZipFolder = formResource.text + @"\" + zipfolder.Name + ".zip";
        string nameFinishZip = targetResource.text + @"\" + zipfolder.Name + ".zip";
        if(File.Exists(nameFinishZip)) File.Delete(nameFinishZip);
        ZipFile.CreateFromDirectory(zipfolder.FullName, nameZipFolder);

        string nameZipFolder2 = zipfolder.FullName +@"\" + zipfolder.Name + ".zip";
        if(File.Exists(nameZipFolder2)) File.Delete(nameZipFolder2);
        File.Move(nameZipFolder, nameZipFolder2);

        string fileZipMetaName = zipfolder.Name;
        var listDataZip = new List<object>();
        long totalSize = 0;
        Dictionary<string, object> metadatazip = new Dictionary<string, object>();
        string nameMetaFile = (targetResource.text + @"\" + zipfolder.Name + "Metadata.txt").Replace("\\", "/");
        WriteZipMetadata(targetResource.text + @"\", fileZipMetaName, listDataZip, fileInfos.Length, totalSize, metadatazip, nameMetaFile, fristAsset.text);

        WriteCSV(listFile, formResource.text + @"\", targetResource.text + @"\", targetResource.text + @"\", fileZipMetaName, progressValue);

    } 


    public static void WriteTextFile(GMetaFile metaFile, string des)
    {
        var desTemp = des;
        var ext = Path.GetExtension(desTemp);

        if (!string.IsNullOrEmpty(ext))
        {
            desTemp = desTemp.Replace(ext, ext.ToLower());
        }

        var json = MiniJson.Serialize(metaFile.ToDic());      
        File.WriteAllText(desTemp, json);
    }

    private void WriteByte(byte[] bytes, string dest)
    {
        var desTemp = dest;
        var ext = Path.GetExtension(desTemp);
        if (!string.IsNullOrEmpty(ext))
        {
            desTemp = desTemp.Replace(ext, ".unity3d");
        }

        File.WriteAllBytes(desTemp, bytes);
    }

    private byte[] Encrypt(byte[] data, int encryptTotal = 1)
    {
        for (var i = 0; i < encryptTotal; i++) 
        {
            data[i] = (byte)~data[i];
        }
        return data;
    }
    
    private FileKind CheckTypeFile(FileInfo file)
    {
        if(file.Name.Contains(".txt")) return FileKind.Text;
        else if(file.Name.Contains(".png")) return FileKind.Image;
            else return FileKind.None;
    }

    public static void WriteZipMetadata(string prePathFileZip, string fileZipName, List<object> listDataZip, int childCount,
            long totalSize, Dictionary<string, object> metadatazip, string nameMetaFile, string desFirstAsset)
    {
        FileInfo fileInfo;
        fileInfo = new FileInfo(prePathFileZip + fileZipName + ".zip");
        if (fileInfo != null)
        {
            listDataZip.Add(AddZipData(fileInfo.Name, fileInfo.Length, childCount));

            totalSize += fileInfo.Length;
        }

        metadatazip.Add("data", listDataZip);

        metadatazip.Add("total_size", totalSize);

        string metadataJson = MiniJson.Serialize(metadatazip);

        File.WriteAllText(nameMetaFile, metadataJson);

        //merge first asset

        DirectoryInfo folderFirstAsset = new DirectoryInfo(desFirstAsset);

        FileInfo[] fileInfos = folderFirstAsset.GetFiles();

        for (int i = 0; i < fileInfos.Length; ++i)
        {
            var dataTarget = File.ReadAllText(fileInfos[i].FullName.Replace("\\", "/"));

            var dicTarget = (Dictionary<string, object>)MiniJson.Deserialize(dataTarget);

            dicTarget["asset2d"] = listDataZip;

            dicTarget["asset2d_size"] = totalSize;

            if (!dicTarget.ContainsKey("assetbundle_size"))
                    dicTarget["assetbundle_size"] = 0;

            dicTarget["total_size"] = long.Parse(dicTarget["assetbundle_size"].ToString()) + totalSize;
                
            string content = MiniJson.Serialize(dicTarget);

            File.WriteAllText(fileInfos[i].FullName.Replace("\\", "/"), content);
        }
    }

    public static Dictionary<string, object> AddZipData(string name, long size, int childCount)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data.Add("assetbundle_name", name);

        data.Add("size", size);

        data.Add("child_count", childCount);

        return data;
    }

    public void WriteCSV(List<GFileView> fileViews, string desFrom, string desTarget,
    string prePathFileZip, string fileZipName, float plusProgress)
    {
        int count = 0;

        var listLine = "v2\n";
        foreach (var fileView in fileViews)
        {
            var ext = Path.GetExtension(fileView.Path);
            Debug.Log(ext);            
            Debug.Log(fileView.Path);            
            var fileId = fileView.Path.Replace(desFrom, "").Replace(ext, "");
            if (fileView is GImageFile)
            {
                var fv = (GImageFile) fileView;
                var asset = new AssetClass()
                {
                    id          = fileId,
                    fileSize    = fv.FileSize,
                    height      = fv.Heigh,
                    width       = fv.Width,
                    PreloadType = fv.PreloadType,
                    CacheType   = fv.CacheType,
                    sign        = fv.Date + "79",
                    type        = ext.Replace(".", "").ToLower()
                };

                listLine += asset.ToImageCSV() + "\n";
            }
            else if (fileView is GTextFile)
            {
                var fv = (GTextFile)fileView;
                var asset = new AssetClass()
                {
                    id          = fileId,
                    fileSize    = fv.FileSize,
                    PreloadType = fv.PreloadType,
                    CacheType   = fv.CacheType,
                    sign = fv.Date + "79",
                    type        = ext.Replace(".", "").ToLower()
                };

                listLine += (asset.ToTextCSV()) + "\n";
            }

            count++;
            progressValue = plusProgress + (count / fileInfos.Length) * 100 / 2;
        }

        var data = System.Text.Encoding.ASCII.GetBytes(listLine);
        var dataEncrypt = Encrypt(data, data.Length);
        WriteByte(dataEncrypt, desTarget + "\\map.unity3d");
        Debug.Log((desTarget + "\\map.unity3d").Replace("\\", "/"));

        using (FileStream zipToOpen = new FileStream(prePathFileZip + fileZipName + ".zip", FileMode.Open))
        {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                archive.CreateEntryFromFile((desTarget + "\\map.unity3d").Replace("\\", "/"), "map.unity3d");
            }
        }
    }


    public static string staticNameFile = "FristBlood";
    public void TickPreload(string Type)
    {
        switch (Type)
        {
            case "Ondemand":
                preloadType[staticNameFile] = GRPreloadType.OnDemand;
                break;

            case "Queue":
                preloadType[staticNameFile] = GRPreloadType.Queue;
                break;

            case "Preload":
                preloadType[staticNameFile] = GRPreloadType.Preload;
                break;

            default: break;
        }
    }

    public void TickCache(string Type)
    {
        switch (Type)
        {
            case "Disk":
                cacheType[staticNameFile] = GRCacheType.Disk;
                break;

            case "RamPersistent":
                cacheType[staticNameFile] = GRCacheType.RamPersistent;
                break;

            case "None":
                cacheType[staticNameFile] = GRCacheType.None;
                break;

            default: break;
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void Update() 
    {
        progressBar.value = progressValue;
    }

    private void AddListFile(FileInfo file)
    {
        var bytes = File.ReadAllBytes(file.FullName);
            var texture = new Texture2D(1,1);
            texture.LoadImage(bytes, false);

            switch (CheckTypeFile(file))
            {
                case FileKind.Image:
                listFile.Add(new GImageFile()
                {
                    Path = file.FullName,
                    FileSize = file.Length,
                    Heigh = texture.height,
                    Width = texture.width,
                    PreloadType = preloadType[file.Name],
                    CacheType = cacheType[file.Name],
                    Date = file.LastWriteTime.ToString("ddMMyyyyHHmmss")
                });
                break;

                case FileKind.Text:
                listFile.Add(new GTextFile()
                {
                    Path = file.FullName,
                    FileSize = file.Length,
                    PreloadType = preloadType[file.Name],
                    CacheType = cacheType[file.Name],
                    Date = file.LastWriteTime.ToString("ddMMyyyyHHmmss")
                });
                break;

                default: break;
            }
    }
}
