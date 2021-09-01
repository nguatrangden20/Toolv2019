using MiniJSON;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Libs
{
    public class GFile
    {
        public string FileName { get; set; }
        public string Path { get; set; }

        public GRPreloadType PreloadType;
        public GRCacheType CacheType;
        public FileKind Kind;
        public long FileSize;
        public string Date;

        public GFile()
        {
            PreloadType = GRPreloadType.Queue;
            CacheType = GRCacheType.Disk;
        }
    }

    [Serializable]
    public class GFileView : GFile
    {
        public FileStatus StatusId;
        public Texture2D StatusImage { get; set; }
        public string ParentFolder { get; set; }
    }

    public class GImageFile : GFileView
    {
        public int Width;
        public int Heigh;
    }

    public class GTextFile : GFileView
    {
    }

    public class GMetaFile : GFile
    {
        public string Id;
        public bool HasChange(GFile metaFile)
        {
            if (metaFile == null) return true;
            if (metaFile.FileSize != FileSize) return true;
            if (metaFile.Date != Date) return true;
            if (metaFile.Kind != Kind) return true;

            return false;
        }

        public Dictionary<string, object> ToDic()
        {
            return new Dictionary<string, object>()
            {
                {"id",          Id},
                {"path",        Path},
                {"file_name",   FileName},
                {"kind",        (int) Kind},
                {"file_size",   FileSize},
                {"date",        Date},
                {"preload",     (int)PreloadType},
                {"cache",       (int)CacheType}
            };
        }

        public GMetaFile Parse(Dictionary<string, object> h)
        {
            Id          = h["id"].ToString();
            Path        = h["path"].ToString();
            FileName    = h["file_name"].ToString();
            Kind        = (FileKind) (int.Parse(h["kind"].ToString()));
            FileSize    = int.Parse(h["file_size"].ToString());
            Date        = h["date"].ToString();
            PreloadType = (GRPreloadType) (int.Parse(h["preload"].ToString()));
            CacheType   = (GRCacheType) (int.Parse(h["cache"].ToString()));
            return this;
        }
    }
    public class AssetClass
    {
        public string id;
        public string type;
        public long fileSize;
        public int width;
        public int height;
        public string sign;
        

        public GRPreloadType PreloadType;
        public GRCacheType CacheType;

        public AssetClass()
        {
            PreloadType = GRPreloadType.Queue;
            CacheType   = GRCacheType.Disk;
        }

        public string ToImageCSV()
        {
            return string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", id, type, sign, fileSize, (int)PreloadType, (int)CacheType, width, height);
        }

        public string ToTextCSV()
        {
            return string.Format("{0};{1};{2};{3};{4};{5}", id, type, sign, fileSize, (int)PreloadType, (int)CacheType);
        }
    }

    public class HostData
    {
        public static Dictionary<string, object> dicHost;
        public static void LoadData()
        {
            string dataText;

            if (File.Exists("hostData.json"))
            {
                dataText = File.ReadAllText("hostData.json");
            }
            else
            {
                dataText = "{  \"hosts\": [    {      \"name\": \"hostT\",      \"path\": \"D:\\\\Test\",      \"url\": \"\",      \"typeConnect\": \"local\",      \"port\": \"\",      \"username\": \"\",      \"password\": \"\",      \"ssh\": \"\"    }  ]}";
            }            

            dicHost = (Dictionary<string, object>)MiniJson.Deserialize(dataText);

            return;
        }

        public static void SaveData()
        {
            string dataText = MiniJson.Serialize(dicHost);

            File.WriteAllText("hostData.json", dataText);
        }
    }

    public enum FileKind
    {
        None = 0, Text = 1 , Image =2
    }

    public enum FileStatus
    {
        New, Edit, Del
    }
    public enum GRPreloadType
    {
        OnDemand,   // only load when being request
        Queue,      // queue up and load while the game is running
        Preload,    // always preload
    }
    public enum GRCacheType
    {
        Disk,           // cache to disk only, do not decode or put in RAM - useful for disk eagerly preload
        RamPersistent,  // keep in ram forever once being loaded, never destroy
        None,           // always reload when being request, do not cache - useful for checking version / update
    }

}
