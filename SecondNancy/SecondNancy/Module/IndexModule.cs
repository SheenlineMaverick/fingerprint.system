namespace FPWS
{
    using Nancy;
    using MySql.Data.MySqlClient;
    using System.Collections.Generic;
    using FPWS.Model;
    using System.IO;
    using System.Linq;
    using SourceAFIS.Simple;
    using System.Drawing;
    using ZXing;
    using ZXing.Common;
    using ZXing.QrCode;
    using System;
    using ZXing.QrCode.Internal;

    public class IndexModule : NancyModule
    {
        static AfisEngine Afis = new AfisEngine();
        static string requestTime;
        static Size realSize = new Size(1380, 1380);

        public IndexModule()
        {
            Get["/"] = parameters =>
            {
                return View["index"];
            };

            Get["/upload"] = parameters =>
            {
                    return View["upload"];
            };

            Get["/certificate"] = parameters =>
            {
                return View["cert"];
            };

            Get["/result"] = parameters =>
            {
                string enhancedQRCodePath = Path.Combine("data", "qrdata") + @"\" + "EnhancedqrImage.bmp";
                Bitmap QRcodeImage = new Bitmap(Bitmap.FromFile(enhancedQRCodePath));

                Rectangle cropArea = new Rectangle();
                cropArea.Width = 357;
                cropArea.Height = 392;
                cropArea.X = (QRcodeImage.Width - cropArea.Width) / 2;
                cropArea.Y = (QRcodeImage.Height - cropArea.Height) / 2;
                Bitmap bmpCrop = QRcodeImage.Clone(cropArea, QRcodeImage.PixelFormat);

                string rawFPImage = Path.Combine("data", "qrdata") + @"\" + "fingerprint.bmp";
                bmpCrop.Save(rawFPImage);

                LuminanceSource source = new BitmapLuminanceSource(QRcodeImage);
                BinaryBitmap newbitmap = new BinaryBitmap(new HybridBinarizer(source));
                Result result = new MultiFormatReader().decodeWithState(newbitmap);

                if (result.Text != requestTime)
                {
                    return Response.AsJson(new { Result = "Authentication failure" });
                }
                else
                {
                    //Write your code here!(next time!)
                    //return Response.AsJson(new { Result = result.Text });
                }

                return Response.AsImage(rawFPImage);
            };

            Get["/securityUpload"] = parameters =>
            {
                return View["secert"];
            };

            Post["/severification"] = parameters =>
            {
                var file = this.Request.Files.ElementAt<HttpFile>(0);
                string uploadPath = Path.Combine("data", "test", file.Name);
                if (!Directory.Exists(Path.Combine("data", "test"))) Directory.CreateDirectory(Path.Combine("data", "test"));
                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    file.Value.CopyTo(fileStream);
                }

                string QRcodePath = Path.Combine("data", "qrdata") + @"\" + "qrImage.bmp";

                Bitmap fpImage = new Bitmap(Bitmap.FromFile(uploadPath));
                Bitmap RawQRImage = new Bitmap(Bitmap.FromFile(QRcodePath));

                Bitmap QRImage = new Bitmap(RawQRImage.Width, RawQRImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(QRImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.DrawImage(RawQRImage, 0, 0);
                }

                int middleImgW = Math.Min((int)(realSize.Width / 3.5), fpImage.Width);
                int middleImgH = Math.Min((int)(realSize.Height / 3.5), fpImage.Height);
                int middleImgL = (QRImage.Width - middleImgW) / 2;
                int middleImgT = (QRImage.Height - middleImgH) / 2;

                System.Drawing.Graphics MyGraphic = System.Drawing.Graphics.FromImage(QRImage);

                MyGraphic.FillRectangle(Brushes.White, middleImgL, middleImgT, middleImgW, middleImgH);
                MyGraphic.DrawImage(fpImage, middleImgL, middleImgT, middleImgW, middleImgH);

                string enhancedQRCodePath = Path.Combine("data", "qrdata") + @"\" + "EnhancedqrImage.bmp";
                QRImage.Save(enhancedQRCodePath);
                return Response.AsImage(enhancedQRCodePath);
            };

            Get["/barcode"] = parameters =>
            {
                BarcodeWriter writer = new BarcodeWriter();
                writer.Format = BarcodeFormat.QR_CODE;
                writer.Options = new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 1840,
                    Height = 1840,
                    ErrorCorrection = ErrorCorrectionLevel.H
                };
                requestTime = DateTime.Now.ToString();
                Bitmap qrImage = writer.Write(requestTime);

                string path = Path.Combine("data", "qrdata");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string imagePath = path + @"\" + "qrImage.bmp";
                qrImage.Save(imagePath);

                return Response.AsImage(imagePath);
            };


            Post["/verification"] = parameters =>
            {
                string userId = (string)this.Request.Form.userId;
                List<Model.Fingerprint> fingerprints = SqlHelper.getImages(userId);

                var file = this.Request.Files.ElementAt<HttpFile>(0);
                string uploadPath = Path.Combine("data", "test", file.Name);
                if (!Directory.Exists(Path.Combine("data", "test"))) Directory.CreateDirectory(Path.Combine("data", "test"));
                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    file.Value.CopyTo(fileStream);
                }
                SourceAFIS.Simple.Fingerprint fp1 = new SourceAFIS.Simple.Fingerprint();
                fp1.AsBitmap = new Bitmap(Bitmap.FromFile(uploadPath));
                Person person1 = new Person();
                person1.Fingerprints.Add(fp1);
                Afis.Extract(person1);

                List<MatchResult> results = new List<MatchResult>();

                foreach (var fp in fingerprints)
                {
                    SourceAFIS.Simple.Fingerprint fp2 = new SourceAFIS.Simple.Fingerprint();
                    fp2.AsBitmap = new Bitmap(Bitmap.FromFile(fp.fpPath));
                    Person person2 = new Person();
                    person2.Fingerprints.Add(fp2);
                    Afis.Extract(person2);


                    MatchResult result = new MatchResult();
                    result.fingerprint = fp.fpName + fp.sampleNumber.ToString();
                    result.score = Afis.Verify(person1, person2);

                    results.Add(result);
                }

                return Response.AsJson<List<MatchResult>>(results);
            };

            Post["/upload"] = parameters =>
            {
                User user = new User();
                Model.Fingerprint fp1 = new Model.Fingerprint();
                Model.Fingerprint fp2 = new Model.Fingerprint();
                Model.Fingerprint fp3 = new Model.Fingerprint();
                Model.Fingerprint fp4 = new Model.Fingerprint();
                Model.Fingerprint fp5 = new Model.Fingerprint();

                user.userId = (string)this.Request.Form.id;
                user.userName = (string)this.Request.Form.name;                

                fp1.fpName = (string)this.Request.Form.fpname1;
                fp2.fpName = (string)this.Request.Form.fpname2;
                fp3.fpName = (string)this.Request.Form.fpname3;
                fp4.fpName = (string)this.Request.Form.fpname4;
                fp5.fpName = (string)this.Request.Form.fpname5;

                fp1.sampleNumber = (int)this.Request.Form.samplenumber1;
                fp2.sampleNumber = (int)this.Request.Form.samplenumber2;
                fp3.sampleNumber = (int)this.Request.Form.samplenumber3;
                fp4.sampleNumber = (int)this.Request.Form.samplenumber4;
                fp5.sampleNumber = (int)this.Request.Form.samplenumber5;

                fp1.userID = user.userId;
                fp2.userID = user.userId;
                fp3.userID = user.userId;
                fp4.userID = user.userId;
                fp5.userID = user.userId;

                fp1.fpID = fp1.userID + fp1.fpName + fp1.sampleNumber.ToString();
                fp2.fpID = fp1.userID + fp2.fpName + fp2.sampleNumber.ToString();
                fp3.fpID = fp3.userID + fp3.fpName + fp3.sampleNumber.ToString();
                fp4.fpID = fp4.userID + fp4.fpName + fp4.sampleNumber.ToString();
                fp5.fpID = fp5.userID + fp5.fpName + fp5.sampleNumber.ToString();

                var file1 = this.Request.Files.ElementAt<HttpFile>(0);
                var file2 = this.Request.Files.ElementAt<HttpFile>(1);
                var file3 = this.Request.Files.ElementAt<HttpFile>(2);
                var file4 = this.Request.Files.ElementAt<HttpFile>(3);
                var file5 = this.Request.Files.ElementAt<HttpFile>(4);

                fp1.fpPath = @"data\" + user.userName + @"\" + fp1.fpID + file1.Name.Substring(file1.Name.Length -4,  4);
                fp2.fpPath = @"data\" + user.userName + @"\" + fp2.fpID + file2.Name.Substring(file2.Name.Length - 4, 4);
                fp3.fpPath = @"data\" + user.userName + @"\" + fp3.fpID + file3.Name.Substring(file3.Name.Length - 4, 4);
                fp4.fpPath = @"data\" + user.userName + @"\" + fp4.fpID + file4.Name.Substring(file4.Name.Length - 4, 4);
                fp5.fpPath = @"data\" + user.userName + @"\" + fp5.fpID + file5.Name.Substring(file5.Name.Length - 4, 4);

                //fp1.fpPath = Path.Combine("data", user.userName, fp1.fpID + file1.Name.Substring(file1.Name.Length - 4, 4));
                if (!Directory.Exists(Path.Combine("data", user.userName))) Directory.CreateDirectory(Path.Combine("data", user.userName));
                using (var fileStream = new FileStream(fp1.fpPath, FileMode.Create))
                {
                    file1.Value.CopyTo(fileStream);
                }
                using (var fileStream = new FileStream(fp2.fpPath, FileMode.Create))
                {
                    file2.Value.CopyTo(fileStream);
                }
                using (var fileStream = new FileStream(fp3.fpPath, FileMode.Create))
                {
                    file3.Value.CopyTo(fileStream);
                }
                using (var fileStream = new FileStream(fp4.fpPath, FileMode.Create))
                {
                    file4.Value.CopyTo(fileStream);
                }
                using (var fileStream = new FileStream(fp5.fpPath, FileMode.Create))
                {
                    file5.Value.CopyTo(fileStream);
                }

                if (!SqlHelper.isExistUser(user)) SqlHelper.insertToUser(user);
                int i1 = SqlHelper.insertToFingerprint(fp1);
                int i2 = SqlHelper.insertToFingerprint(fp2);
                int i3 = SqlHelper.insertToFingerprint(fp3);
                int i4 = SqlHelper.insertToFingerprint(fp4);
                int i5 = SqlHelper.insertToFingerprint(fp5);
                if (i1!=0 && i2!=0 && i3!=0 && i4!=0 && i5!=0)
                    return Response.AsJson(new { Result = "Insert Sucess" });
                else
                    return Response.AsJson(new { Result = "Insert Failed" });
            };

            Get["/getUser"] = parameters =>
            {
                string myconn = "Database='fingerprint';Data Source=localhost;User ID=lich;Password=123456;CharSet=utf8;";
                string mysql = "SELECT * from user";
                MySqlConnection myconnection = new MySqlConnection(myconn);
                myconnection.Open();
                MySqlCommand mycommand = new MySqlCommand(mysql, myconnection);
                MySqlDataReader myreader = mycommand.ExecuteReader();
                
                
                List<User> userList = new List<User>();
                while (myreader.Read())
                {
                    User user = new User();
                    user.userId = myreader.GetString(0);
                    user.userName = myreader.GetString(1);
                    userList.Add(user);
                }
                myreader.Close();
                myconnection.Close();                
                return Response.AsJson<List<User>>(userList);
            };

            Get["/inserttest"] = parameters =>
            {
                User user = new User();
                user.userId = "13T2001";
                user.userName = "Huang Wei";
                int i = SqlHelper.insertToUser(user);
                if (i != 0) return Response.AsJson(new { Result = "Insert Sucess" });
                else return Response.AsJson(new { Result = "Insert Failed" });
            };
        }
    }

    
}