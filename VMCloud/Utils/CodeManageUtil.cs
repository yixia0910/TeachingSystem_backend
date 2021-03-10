using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Mvc;

namespace VMCloud.Utils
{
    public class CodeManageUtil
    {
        public static string GetCode()
        {
            string code = GetNumAndStr(4);
            return code;
        }

        public static string GetCodePic(string code)
        {
            //string code = GetRndStr();
            //string code = GetNumAndStr(4);
            //byte[] imageByte;
            string img64;
            using (Bitmap img = CreateImages(code, "ch"))
            {
                //imageByte = BitmapToByte(img);
                img64 = ToBase64(img);
            }
            //return imageByte;
            return img64;
        }
        public static byte[] BitmapToByte(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                byte[] data = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(data, 0, Convert.ToInt32(stream.Length));
                return data;
            }
        }
        /**
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }**/
        /// <summary>
        /// 数字随机数
        /// </summary>
        /// <returns></returns>
        private static string GetRndNum()
        {
            string code = string.Empty;
            Random random = new Random();
            for (int i = 0; i < 4; i++)
            {
                code += random.Next(9);
            }
            return code;
        }
        /// <summary>
        /// 英文随机
        /// </summary>
        /// <returns></returns>
        private static string GetRndStr()
        {
            string Vchar = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
            string[] VcArray = Vchar.Split(',');
            string checkCode = string.Empty;
            Random rand = new Random();
            for (int i = 0; i < 4; i++)
            {
                int t = rand.Next(VcArray.Length);
                checkCode += VcArray[t];
            }
            return checkCode;
        }
        /// <summary>
        /// 中文随机
        /// </summary>
        /// <returns></returns>
        private static string GetRndCh()
        {
            System.Text.Encoding gb = System.Text.Encoding.Default;//获取GB2312编码页（表）
            object[] bytes = CreateRegionCode(4);//生4个随机中文汉字编码
            string[] str = new string[4];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                //根据汉字编码的字节数组解码出中文汉字
                str[i] = gb.GetString((byte[])Convert.ChangeType(bytes[i], typeof(byte[])));
                sb.Append(str[i].ToString());
            }
            return sb.ToString();
        }
        /// <summary>
        /// 产生随机中文字符
        /// </summary>
        /// <param name="strlength"></param>
        /// <returns></returns>
        private static object[] CreateRegionCode(int strlength)
        {
            //定义一个字符串数组储存汉字编码的组成元素
            string[] rBase = new String[16] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            Random rnd = new Random();
            object[] bytes = new object[strlength];

            for (int i = 0; i < strlength; i++)
            {
                //区位码第1位
                int r1 = rnd.Next(11, 14);
                string str_r1 = rBase[r1].Trim();
                //区位码第2位
                rnd = new Random(r1 * unchecked((int)DateTime.Now.Ticks) + i);
                int r2;
                if (r1 == 13)
                {
                    r2 = rnd.Next(0, 7);
                }
                else
                {
                    r2 = rnd.Next(0, 16);
                }
                string str_r2 = rBase[r2].Trim();

                //区位码第3位
                rnd = new Random(r2 * unchecked((int)DateTime.Now.Ticks) + i);//更换随机种子
                int r3 = rnd.Next(10, 16);
                string str_r3 = rBase[r3].Trim();

                //区位码第4位
                rnd = new Random(r3 * unchecked((int)DateTime.Now.Ticks) + i);
                int r4;
                if (r3 == 10)
                {
                    r4 = rnd.Next(1, 16);
                }
                else if (r3 == 15)
                {
                    r4 = rnd.Next(0, 15);
                }
                else
                {
                    r4 = rnd.Next(0, 16);
                }
                string str_r4 = rBase[r4].Trim();
                //定义两个字节变量存储产生的随机汉字区位码
                byte byte1 = Convert.ToByte(str_r1 + str_r2, 16);
                byte byte2 = Convert.ToByte(str_r3 + str_r4, 16);

                //将两个字节变量存储在字节数组中
                byte[] str_r = new byte[] { byte1, byte2 };

                //将产生的一个汉字的字节数组放入object数组中
                bytes.SetValue(str_r, i);
            }
            return bytes;
        }

        public static string GetNumAndStr(int length)
        {
            string str = string.Empty;
            string Vchar = "2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,j,k,m,n,p" +
            ",q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,J,K,L,M,N,P,Q" +
            ",R,S,T,U,V,W,X,Y,Z";

            string[] VcArray = Vchar.Split(new Char[] { ',' });//拆分成数组
            string[] num = new string[length];

            int temp = -1;//记录上次随机数值，尽量避避免生产几个一样的随机数

            Random rand = new Random();
            //采用一个简单的算法以保证生成随机数的不同
            for (int i = 1; i < length + 1; i++)
            {
                if (temp != -1)
                {
                    rand = new Random(i * temp * unchecked((int)DateTime.Now.Ticks));
                }

                int t = rand.Next(VcArray.Length);

                temp = t;
                str += VcArray[t];
                // num[i - 1] = VcArray[t]; 
            }
            return str;
        }
        /// <summary>
        /// 画图片的背景图+干扰线 
        /// </summary>
        /// <param name="checkCode"></param>
        /// <returns></returns>
        private static Bitmap CreateImages(string checkCode, string type)
        {
            int step = 0;
            if (type == "ch")
            {
                step = 5;//中文字符，边界值做大
            }
            int iwidth = (int)(checkCode.Length * (22 + step));
            System.Drawing.Bitmap image = new System.Drawing.Bitmap(iwidth, 40);
            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);//清除背景色
            Color[] c = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Orange, Color.Brown, Color.DarkCyan, Color.Purple };//定义随机颜色
            string[] font = { "Verdana", "Microsoft Sans Serif", "Comic Sans MS", "Arial", "宋体" };
            Random rand = new Random();

            for (int i = 0; i < 50; i++)
            {
                int x1 = rand.Next(image.Width);
                int x2 = rand.Next(image.Width);
                int y1 = rand.Next(image.Height);
                int y2 = rand.Next(image.Height);
                g.DrawLine(new Pen(Color.LightGray, 1), x1, y1, x2, y2);//根据坐标画线
            }

            for (int i = 0; i < checkCode.Length; i++)
            {
                int cindex = rand.Next(7);
                int findex = rand.Next(5);

                Font f = new System.Drawing.Font(font[findex], 20, System.Drawing.FontStyle.Bold);
                Brush b = new System.Drawing.SolidBrush(c[cindex]);
                int ii = 4;
                if ((i + 1) % 2 == 0)
                {
                    ii = 2;
                }
                g.DrawString(checkCode.Substring(i, 1), f, b, 6 + (i * (20 + step)), ii);

            }
            g.DrawRectangle(new Pen(Color.Silver, 0), 0, 0, image.Width - 1, image.Height - 1);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            return image;
        }

        /// <summary>
        /// bmp转base64
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        private static string ToBase64(Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();
            String strbaser64 = Convert.ToBase64String(arr);
            return strbaser64;
        }
    }
}