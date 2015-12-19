using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace FPWS.Model
{
    class SqlHelper
    {
        static string myconn = "Database='fingerprint';Data Source=localhost;User ID=lich;Password=123456;CharSet=utf8;";

        public static int insertToUser(User user)
        {
            int rows;
            using (MySqlConnection myconnection = new MySqlConnection(myconn))
            {
                string commandText = "insert into user values (@userId,@userName)";
                MySqlCommand command = new MySqlCommand(commandText,myconnection);
                command.Parameters.AddWithValue("@userId", user.userId);
                command.Parameters.AddWithValue("@userName", user.userName);
                myconnection.Open();
                rows = command.ExecuteNonQuery();
            }
            return rows;
        }

        public static int insertToFingerprint(Fingerprint fp)
        {
            int rows;
            using (MySqlConnection myconnection = new MySqlConnection(myconn))
            {
                string commandText = "insert into fingerprint values (@fpID,@fpName,@sampleNumber,@userID,@fpPath)";
                MySqlCommand command = new MySqlCommand(commandText, myconnection);
                command.Parameters.AddWithValue("@fpID", fp.fpID);
                command.Parameters.AddWithValue("@fpName", fp.fpName);
                command.Parameters.AddWithValue("@sampleNumber", fp.sampleNumber);
                command.Parameters.AddWithValue("@userID", fp.userID);
                command.Parameters.AddWithValue("@fpPath", fp.fpPath);
                myconnection.Open();
                rows = command.ExecuteNonQuery();
            }
            return rows;
        }

        public static bool isExistUser(User user)
        {
            int num;
            using(MySqlConnection myconnection = new MySqlConnection(myconn))
            {
                string commandText = "select count(*) from user where userId= @userId";
                MySqlCommand command = new MySqlCommand(commandText, myconnection);
                command.Parameters.AddWithValue("@userId", user.userId);
                myconnection.Open();
                num = Convert.ToInt32(command.ExecuteScalar());
            }
            if (num != 0) return true;
            else return false;
        }

        public static List<Model.Fingerprint> getImages(string userId)
        {
            List<Model.Fingerprint> images = new List<Model.Fingerprint>();
            using (MySqlConnection myconnection = new MySqlConnection(myconn))
            {
                string commandText = "select * from fingerprint where userId=@userId";
                MySqlCommand command = new MySqlCommand(commandText, myconnection);
                command.Parameters.AddWithValue("@userId", userId);
                myconnection.Open();
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Model.Fingerprint fp = new Model.Fingerprint();
                        fp.fpID = reader.GetString(0);
                        fp.fpName = reader.GetString(1);
                        fp.sampleNumber = reader.GetInt32(2);
                        fp.userID = reader.GetString(3);
                        fp.fpPath = reader.GetString(4);
                        images.Add(fp);
                    }
                }
            }
            return images;
        }
    }
}
