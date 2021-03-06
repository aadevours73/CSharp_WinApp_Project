using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace FTP_Client_Demo
{
//===================================FTP 서버에 접속하는 작업을 처리하는 클래스==============================
    class FTP_Access
    {
        //델리게이트
        public delegate void ExceptionEventHandler(string locationID, Exception ex);

        //에러처리는 해야져..ㅎ
        public event ExceptionEventHandler ExceptionEvent;
        public Exception LastException = null;

        public bool Is_Connected {get; set;}
        //서버 접속에 필요한 정보들 저장하는 변수들.
        private string IP;
        private string port;
        private string user_ID;
        private string user_PW;

        //다운로드, 업로드 현황 표기를 위한 변수들.
        private int FullSize;
        private int DownloadSize;
        private int UploadSize;

        public FTP_Access() { }

        //ftp 서버에 연결하는 메소드
        public bool Connect_FTP_Server(string ip, string port, string id, string password)
        {
            this.Is_Connected = false;

            this.IP = ip;
            this.port = port;
            this.user_ID = id;
            this.user_PW = password;

            string URL_Addr = string.Format("FTP://{0}:{1}/", this.IP, this.port);
            try
            {
                //FTP 클라 생성
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(URL_Addr);
                request.Credentials = new NetworkCredential(this.user_ID, this.user_PW);

                request.KeepAlive = false;
                //폴더내용 받아오기로 메소드 설정.
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

                request.UsePassive = false;

                //응답을 받아온다.
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                //받은 응답에서 스트림을 가져와 읽는다.
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string[] data = reader.ReadToEnd().Split('\n');


                this.Is_Connected = true;
            }
            catch (Exception ex) {
                this.LastException = ex;
                //멤버 특정 정보 가져오기

                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod();
                string info_id = string.Format("{0}.{1}", info.ReflectedType.Name, info.Name);

                if (this.ExceptionEvent != null)
                {
                    this.ExceptionEvent(id, ex);
                }
                return false;
            }
            return true;
        }

        //지정한 경로에 해당하는 파일정보 리스트들을 불러오는 함수
        public List<string[]> get_File_List(string PATH) {
            string URL = string.Format("FTP://{0}:{1}{2}", this.IP, this.port, PATH);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(URL);
            request.Credentials = new NetworkCredential(this.user_ID, this.user_PW);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            //응답을 받아온다.
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            //받은 응답에서 스트림을 가져와 읽는다.
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string[] raw_fileInfo = reader.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<string[]> file_list = new List<string[]>();//반환할 파일정보 리스트.

            foreach (string file in raw_fileInfo)
            {
                //fileDetailes = {날짜, 용량(폴더라면 <DIR>), 파일이름}
                string date = file.Substring(0, 17);
                string Capacity = file.Substring(17, 21).Trim();
                string name = file.Substring(39);
                string[] fileDetailes = { date, Capacity, name };
                file_list.Add(fileDetailes);
            }

            return file_list;
        }

        public bool File_DownLoad(string localFullDownLoadPath, string serverCurrentPath, string FileName) {
            try {
                string URL = string.Format("FTP://{0}:{1}{2}/{3}", this.IP, this.port, serverCurrentPath, FileName);
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(URL);
                request.Credentials = new NetworkCredential(this.user_ID, this.user_PW);//인증정보
                request.KeepAlive = false;//연결 살려둘거에요?
                request.UseBinary = true;//이진형식을 Data주고받을거에요?
                request.UsePassive = false;//클라가 Data포트 연결 시작해야하나요?

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();//응답 받아옴.

                string FullDownLoadFile = localFullDownLoadPath + "/" + FileName;//로컬에 파일 어디에 저장할지
                FileStream outputStream = new FileStream(FullDownLoadFile, FileMode.Create, FileAccess.Write);//파일 작성에 쓸 스트림.
                Stream ftpStream = response.GetResponseStream();//가져온 응답을 다룰 스트림.

                int bufferSize = 2048;//버퍼사이즈 설정.
                int readCount;//얼마나 많은 버퍼를 읽을지를 지정해주는 변수.
                byte[] buffer = new byte[bufferSize];

                //처음 1번 읽어온 뒤에 ftpStream에서 파일을 읽는데 몇개버퍼나 더 읽어와야하는지 구한다.
                readCount = ftpStream.Read(buffer, 0, bufferSize);
                FullSize = readCount + 1;//ref로 받아온 총 받아야할 버퍼 갯수변수에 값 집어넣기.
                DownloadSize = 1;//ref로 받아온 현재 보낸 버퍼 갯수에 값집어넣기

                while (readCount > 0) { //readCount가 0이 될때까지 반복한다.
                    outputStream.Write(buffer, 0, readCount);//파일작성 스트림으로 지정한 경로에 readCount순서대로 내용을 작성한다.
                    readCount = ftpStream.Read(buffer, 0, bufferSize);//1번 더 읽어오고 readCount를 갱신.
                    DownloadSize++;//현재 보낸 버퍼 갯수 업데이트
                }

                //파일 쓰는거 다 끝나면 스트림 다 닫는다.
                ftpStream.Close();
                outputStream.Close();

                if (buffer != null) {//버퍼에 찌꺼기가 껴있으면
                    Array.Clear(buffer, 0, buffer.Length);//청소한다.
                    buffer = null;
                }

                //가져온 사이즈 변수들도 초기화 해준다.
                FullSize = 0;
                DownloadSize = 0;

                return true;
            }
            catch (Exception ex)//에러처리
            {
                this.LastException = ex;

                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod();
                string id = string.Format("{0}.{1}", info.ReflectedType.Name, info.Name);

                if(this.ExceptionEvent != null)
                {
                    this.ExceptionEvent(id, ex);
                }
                return false;
            }
        }

        public bool File_UpLoad(string localUpLoadPath, string serverCurrentPath) {
            try
            {
                string Local_File_Name = Path.GetFileName(localUpLoadPath);
                string FTP_URL = string.Format("FTP://{0}:{1}{2}/{3}", this.IP, this.port, serverCurrentPath, Local_File_Name);


                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FTP_URL);
                request.Credentials = new NetworkCredential(this.user_ID, this.user_PW);//인증정보
                request.Method = WebRequestMethods.Ftp.UploadFile;//업로드 지정.

                FileStream sourceFileStream = new FileStream(localUpLoadPath, FileMode.Open, FileAccess.Read);
                Stream TargetWriteStream = request.GetRequestStream();

                int bufflength = 2048;//2048 바이트씩 읽어서 보낸다.
                byte[] buff = new byte[bufflength];//버퍼배열선언.


                FullSize = (int)sourceFileStream.Length / bufflength;
                UploadSize = 0;
                while (true)
                {
                    int byteCount = sourceFileStream.Read(buff, 0, buff.Length);

                    if (byteCount == 0)
                        break;
                    TargetWriteStream.Write(buff, 0, byteCount);
                    UploadSize++;
                }
                TargetWriteStream.Close();
                sourceFileStream.Close();


                if (buff != null)
                {//버퍼에 찌꺼기가 껴있으면
                    Array.Clear(buff, 0, buff.Length);//청소한다.
                    buff = null;
                }
            }
            catch (Exception ex)
            {
                this.LastException = ex;

                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod();
                string id = string.Format("{0}.{1}", info.ReflectedType.Name, info.Name);



                if (this.ExceptionEvent != null)
                {
                    this.ExceptionEvent(id, ex);
                }
                return false;
            }
            return true;
        }

        public bool New_Folder(string serverCurrentPath, string Folder_Name)
        {
            return true;
        }

        public int getFullSize() {
            return FullSize;
        }

        public int getDownloadSize() {
            return DownloadSize;
        }
    }
}
