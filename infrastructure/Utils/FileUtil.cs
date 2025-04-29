using domain.Enums;
using Microsoft.AspNetCore.Http;

namespace infrastructure.Utils;

/// <summary>
/// 文件工具类
/// </summary>
public class FileUtil
{
    static string[] imgs = { ".jpg", ".jpeg", ".png", ".xbm", ".tif", ".pjp", ".svgz", ".jpg",
        ".jpeg", ".ico", ".tiff", ".gif", ".svg", ".jfif", ".webp", ".png", ".bmp", ".pjpeg", ".avif" };
    static string[] txts = { ".txt", ".text",  };
    static string[] excels = { ".xls", ".xlsx",  };
    
    /// <summary>
    /// 创建文件夹路径默认 FileUpload文件夹路径下
    /// </summary>
    /// <returns></returns>
    public static string CreatedDirPath(string? basePath)
    {
        if (basePath == null)
        {
            return $"FileUpload{Path.DirectorySeparatorChar}{DateTime.Now:yyyy-MM-dd}";
        }
        return $"{basePath}{Path.DirectorySeparatorChar}FileUpload{Path.DirectorySeparatorChar}{DateTime.Now:yyyy-MM-dd}";
    }

    /// <summary>
    /// 获取文件类型
    /// </summary>
    /// <param name="fileNameEx"></param>
    /// <returns></returns>
    public static FileTypeEnum GetIFormFileType(string fileNameEx)
    {
        foreach (var item in imgs)
        {
            if (fileNameEx.Equals(item))
            {
                return FileTypeEnum.Image;
            }
        }
        foreach (var txt in txts)
        {
            if (fileNameEx.Equals(txt))
            {
                return FileTypeEnum.Txt;
            }
        }
        foreach (var excel in excels)
        {
            if (fileNameEx.Equals(excel))
            {
                return FileTypeEnum.Excel;
            }
        }
        
        return FileTypeEnum.Other;
    }
    /// <summary>
    /// 保存文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fullFileName"></param>
    /// <param name="file"></param>
    public static void SaveFileLocal(string filePath, string fullFileName, IFormFile file)
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        string fullFilePath = $"{filePath}{Path.DirectorySeparatorChar}{fullFileName}";
        if (File.Exists(fullFilePath))
        {
            File.Delete(fullFilePath);
        }
        using (Stream stream = file.OpenReadStream())
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            FileStream fs = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(bytes);
            bw.Close();
            fs.Close();
        }
    }

    
}