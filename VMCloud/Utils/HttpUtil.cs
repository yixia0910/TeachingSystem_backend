/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/8/5 2:16:21
*   Description:  
 */
using Aspose.Cells;
using Aspose.Slides;
using Aspose.Words;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace VMCloud.Utils
{
    public class HttpUtil
    {
        private static HttpClient client;
        private static string baseUriString = ConfigurationManager.AppSettings["AcloudAddress"];

        static HttpUtil()
        {
            var handler = new WebRequestHandler();
            handler.ServerCertificateValidationCallback = delegate { return true; };
            client = new HttpClient(handler);
        }


        public static readonly Dictionary<int, string> Message = new Dictionary<int, string>
        {
            {1001,"成功" },
            {2001,"用户未登录" },
            {2002,"未获取到访问权限" },
            {3001,"请求参数非法" },
            {4001,"服务器内部错误，请联系管理员" }
        };

        public static HttpResponseMessage Method(HttpMethod method, string uriStr,string token = null, object data = null)
        {
            Uri uri = new Uri(new Uri(baseUriString), uriStr);
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = method;
            request.RequestUri = uri;
            if (data != null)
                request.Content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.GetEncoding("UTF-8"), "application/json");
            if (token != null)
            {
                request.Headers.Add("X-Auth-Token", token);
            }
            return client.SendAsync(request).Result;
        }

        public static IEnumerable<object> GetHeader(HttpRequestMessage httpRequest, string headerName)
        {
            IEnumerable<string> headers;
            bool hasHeader = httpRequest.Headers.TryGetValues(headerName, out headers);
            return headers;
        }

        public static IEnumerable<object> GetHeader(HttpResponseMessage httpRequest, string headerName)
        {
            IEnumerable<string> headers;
            bool hasHeader = httpRequest.Headers.TryGetValues(headerName, out headers);
            return headers;
        }

        public static string GetIP(HttpRequestMessage request)
        {
            HttpRequestBase httpRequest = HttpUtil.Convert(request);
            if (httpRequest != null)
            {
                return httpRequest.UserHostAddress;
            }
            else
            {
                return null;
            }
        }

        public static HttpRequestBase Convert(HttpRequestMessage requestMessage)
        {

            HttpContextBase context;
            try
            {
                context = (HttpContextBase)requestMessage.Properties["MS_HttpContext"];
            }
            catch
            {
                return null;
            }
            HttpRequestBase httpRequest = context.Request;
            return httpRequest;
        }

        public static dynamic Deserialize(JObject obj)
        {
            string jsonStr = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<dynamic>(jsonStr);
        }

        public static string GetAuthorization(HttpRequestMessage request)
        {
            if (request.Headers == null || request.Headers.Authorization == null)
                return null;
            else
                return request.Headers.Authorization.ToString();
        }

        public static VMCloud.Models.File UploadFile(HttpPostedFileBase postedFile, string uploaderId, string fileType, string basePath, HttpRequestMessage request)
        {
            string uuid = Guid.NewGuid().ToString();
            string[] fileNameSlice = postedFile.FileName.Split('.');
            string fileExt = fileNameSlice[fileNameSlice.Length - 1];
            string previewUuid = Guid.NewGuid().ToString();
            VMCloud.Models.File newFile = new VMCloud.Models.File
            {
                id = uuid,
                name = postedFile.FileName,

                upload_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                type = fileType,
                size = HttpUtil.B2KBorMB(postedFile.ContentLength),
                path = basePath + "\\" + postedFile.FileName + uuid + "." + fileExt,
                uploader = uploaderId,
            };
            postedFile.SaveAs(newFile.path);
            try
            {
                bool convertOK = HttpUtil.ConvertPreview(newFile, basePath + "\\" + previewUuid + ".pdf");
                if (convertOK)
                    newFile.preview = basePath + "\\" + previewUuid + ".pdf";
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, request);
            }
            return newFile;
        }

        public static HttpResponseMessage DownloadFile(string filePath, string fileName, bool download = true)
        {
            HttpResponseMessage res = new HttpResponseMessage();
            var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open);
            res.Content = new StreamContent(fileStream);
            if (download)
            {
                res.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                res.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
            }
            else
                res.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            {
                res.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline")
                {
                    FileName = fileName
                };
            }
            res.Content.Headers.ContentType.CharSet = "utf-8";
            return res;
        }

        public static bool ConvertPreview(VMCloud.Models.File file, string savePath)
        {
            string ext = GetFileExtensioName(file.name);
            switch (ext)
            {
                case "doc":
                case "docx":
                    Document document = new Document(file.path);
                    document.Save(savePath, Aspose.Words.SaveFormat.Pdf);
                    break;
                case "pdf":
                    Aspose.Pdf.Document pdf = new Aspose.Pdf.Document(file.path);
                    pdf.Save(savePath, Aspose.Pdf.SaveFormat.Pdf);
                    break;
                case "ppt":
                case "pptx":
                    Presentation ppt = new Presentation(file.path);
                    ppt.Save(savePath, Aspose.Slides.Export.SaveFormat.Pdf);
                    break;
                case "xls":
                case "xlsx":
                    Workbook book = new Workbook(file.path);
                    book.Save(savePath, Aspose.Cells.SaveFormat.Pdf);
                    break;
                default:
                    return false;
            }
            return true;
        }

        public static string GetFileExtensioName(string fileName)
        {
            string[] fileNameSlice = fileName.Split('.');
            string ext = fileNameSlice[fileNameSlice.Length - 1];
            return ext;
        }

        public static void StringToFile(string path, string str)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(str);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }

        private static string B2KBorMB(double fileSizeB)
        {
            double fileSizeKB = fileSizeB / 1024;
            if (fileSizeKB < 1024)
                return fileSizeKB.ToString("f2") + "KB";
            double fileSizeMB = fileSizeKB / 1024;
            if (fileSizeMB < 1024)
                return fileSizeMB.ToString("f2") + "MB";
            double fileSizeGB = fileSizeMB / 1024;
            return fileSizeGB.ToString("f2") + "GB";
        }
        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="destinationZipFilePath"></param>
        public static void CreateZip(string sourceFilePath, string destinationZipFilePath)
        {
            if (sourceFilePath[sourceFilePath.Length - 1] != System.IO.Path.DirectorySeparatorChar)
                sourceFilePath += System.IO.Path.DirectorySeparatorChar;

            ZipOutputStream zipStream = new ZipOutputStream(System.IO.File.Create(destinationZipFilePath));
            zipStream.SetLevel(6);  // 压缩级别 0-9
            CreateZipFiles(sourceFilePath, zipStream, sourceFilePath);

            zipStream.Finish();
            zipStream.Close();
        }

        /// <summary>
        /// 递归压缩文件
        /// </summary>
        /// <param name="sourceFilePath">待压缩的文件或文件夹路径</param>
        /// <param name="zipStream">打包结果的zip文件路径（类似 D:\WorkSpace\a.zip）,全路径包括文件名和.zip扩展名</param>
        /// <param name="staticFile"></param>
        private static void CreateZipFiles(string sourceFilePath, ZipOutputStream zipStream, string staticFile)
        {
            Crc32 crc = new Crc32();
            string[] filesArray = Directory.GetFileSystemEntries(sourceFilePath);
            foreach (string file in filesArray)
            {
                if (Directory.Exists(file))                     //如果当前是文件夹，递归
                {
                    CreateZipFiles(file, zipStream, staticFile);
                }

                else                                            //如果是文件，开始压缩
                {
                    FileStream fileStream = System.IO.File.OpenRead(file);

                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    string tempFile = file.Substring(staticFile.LastIndexOf("\\") + 1);
                    ZipEntry entry = new ZipEntry(tempFile);

                    entry.DateTime = DateTime.Now;
                    entry.Size = fileStream.Length;
                    fileStream.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    zipStream.PutNextEntry(entry);

                    zipStream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public static void CreatePreview(Models.File file, string savePath)
        {
            if (file.name.EndsWith(".doc") || file.name.EndsWith(".docx"))
            {
                Workbook workbook = new Workbook();

            }
        }

        public static string Encrypt(String content, Encoding encode)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();//创建SHA1对象
            byte[] bytes_in = encode.GetBytes(content);//将待加密字符串转为byte类型
            byte[] bytes_out = sha1.ComputeHash(bytes_in);//Hash运算
            sha1.Dispose();//释放当前实例使用的所有资源
            string result = BitConverter.ToString(bytes_out);//将运算结果转为string类型
            result = result.Replace("-", "").ToUpper();
            return result;
        }

        /// <summary>
        /// 如果返回值为True,代表传入的time晚于现在
        /// </summary>
        /// <param name="time">要比较的时间</param>
        /// <param name="addDays">延后天数</param>
        /// <param name="addHours">延后小时数</param>
        /// <param name="addMinutes">延后分钟数</param>
        /// <returns></returns>
        public static bool IsTimeLater(string time, int addDays = 0, int addHours = 0, int addMinutes = 0)
        {
            DateTime dt = DateTime.Parse(time);
            dt = dt.AddDays(addDays);
            dt = dt.AddHours(addHours);
            dt = dt.AddMinutes(addMinutes);
            return (dt.CompareTo(DateTime.Now) > 0);
        }

    }
}