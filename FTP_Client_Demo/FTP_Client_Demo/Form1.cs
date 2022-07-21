﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Net;


namespace FTP_Client_Demo
{
    public partial class Form1 : Form
    {
        private FTP_Access FTP = null;
        private Thread th;
        //각 ip주소, 포트번호와 id, pw를 저장할 txt파일 경로

        public Form1()
        {
            InitializeComponent();
            
        }

        //폼이 로드되면 기존에 저장돼있던 ip, 포트를 dat에서 읽어와서 ip입력창, 포트입력창에 띄운다.
        //서버에 연결되기 전엔 쓸 수 없는 파일 목록란과 파일업로드ui, 작업진행상황 프로그레스바를 비활성화 시킨다.
        private void Form_Load(object sender, EventArgs e)
        {
            StreamReader IP_Port_Log = new StreamReader("IP_Port_Log.txt");
            StreamReader ID_PW_Log = new StreamReader("ID_PW_Log.txt");
            for(int i = 0;i<2;i++){
                switch (i) { 
                    case 0:
                        IP_Address_Input.Text = IP_Port_Log.ReadLine();
                        Account_ID.Text = ID_PW_Log.ReadLine();
                        if (IP_Address_Input.Text.Length > 0)//이미 가져올 정보가 있다면
                            Remember_Addr.Checked = true;//주소기억하기를 체크상태로 바꾼다.
                        break;
                    case 1:
                        Port.Text = IP_Port_Log.ReadLine();
                        Password.Text = ID_PW_Log.ReadLine();
                        if (Port.Text.Length > 0)
                            Remember_Addr.Checked = true;
                        break;
                }
            }

            File_InFo_GridView.Enabled = false;
            progressBar1.Enabled = false;
            File_Upload_Button.Enabled = false;
            Find_FilePath_Button.Enabled = false;
            Upload_FilePath.Enabled = false;
        }

        //파일주소 찾아서 업로드 파일경로에 집어넣는다.
        private void Find_FilePath_Button_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            Upload_FileDialog.InitialDirectory = "C:\\";
            if (Upload_FileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = Upload_FileDialog.FileName;
            }
            Upload_FilePath.Text = filePath;
        }

        //연결을 시도한다. 연결이 성공하고 이 주소 기억하기 체크박스가 체크 돼있으면 dat파일에 ip주소와 포트를 작성한다.
        private void Connection_Button_Click(object sender, EventArgs e)
        {

            if (Connection_Button.Text.Equals("연결"))
            {
                //FTP 연결 객체 생성.
                FTP = new FTP_Access();
                //연결 시도하기 전에 ip주소나 다른 입력들이 제대로 입력됐는지 확인한다.
                if (string.IsNullOrEmpty(IP_Address_Input.Text))
                {
                    MessageBox.Show("ip주소를 입력하세요.");
                    IP_Address_Input.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(Port.Text))
                {
                    MessageBox.Show("포트번호를 입력하세요.");
                    Port.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(Account_ID.Text))
                {
                    MessageBox.Show("아이디를 입력해주세요.");
                    Account_ID.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(Password.Text))
                {
                    MessageBox.Show("비밀번호를 입력해주세요.");
                    Password.Focus();
                    return;
                }

//폴더 경로 텍스트박스에 있던 텍스트를 DirectoryInfo 형식으로 선언해 Exists로 해당 경로가 유효한지 확인한다.
                DirectoryInfo dir_info = new DirectoryInfo(Download_Dir_Path.Text);
                if (!dir_info.Exists) 
                {
                    MessageBox.Show("폴더 경로를 제대로 입력해주세요.");
                    Download_Dir_Path.Focus();
                    return;
                }

                //서버에 연결을 시도하고 성공 여부를 불린변수로 받아온다.
                bool success = FTP.Connect_FTP_Server(IP_Address_Input.Text, Port.Text, Account_ID.Text, Password.Text);
                if (success)//성공하면 비활성화된 파일 목록란과 파일업로드ui, 작업진행상황 프로그레스바를 활성화시키고 접속정보 입력란 비활성화와 함께 접속버튼 텍스트를 변경한다.
                {
                    Server_statement.Text = "연결 상태 : 연결성공";
                    Connection_Button.Text = "연결해제";
                    File_InFo_GridView.Enabled = true;//비활성화 시켰던 친구들 활성화
                    progressBar1.Enabled = true;
                    File_Upload_Button.Enabled = true;
                    Find_FilePath_Button.Enabled = true;
                    Upload_FilePath.Enabled = true;

                    IP_Address_Input.Enabled = false;//활성화 돼있던 친구들 비활성화
                    Port.Enabled = false;
                    Account_ID.Enabled = false;
                    Password.Enabled = false;
                    Download_Dir_Path.Enabled = false;

                    //이 주소 기억하기가 체크돼있으면 파일에 해당 정보를 집어넣는다. 체크 안돼있으면 기존 텍스트파일은 삭제한다.
                    if (Remember_Addr.Checked)
                    {
                        StreamWriter sw = new StreamWriter("IP_Port_Log.txt");
                        sw.WriteLine(IP_Address_Input.Text);
                        sw.WriteLine(Port.Text);
                        sw.Close();
                    }
                    else
                        File.Delete("IP_Port_Log.txt");

                    //이 로그인 정보 기억하기가 체크돼있으면 파일에 해당 정보를 집어넣는다. 체크 안돼있으면 기존 텍스트파일은 삭제한다.
                    if (Remember_Addr.Checked)
                    {
                        StreamWriter sw = new StreamWriter("ID_PW_Log.txt");
                        sw.WriteLine(Account_ID.Text);
                        sw.WriteLine(Password.Text);
                        sw.Close();
                    }
                    else
                        File.Delete("ID_PW_Log.txt");

                }
                else//실패시
                {
                    MessageBox.Show("연결에 실패했습니다. 올바르게 입력했는지 확인해주세요.");
                    return;
                }
            }
            else {//연결버튼이 연결해제 상태일때

                FTP = null;//FTP객체 박살내기
                Server_statement.Text = "연결 상태 : 연결안됨";
                Connection_Button.Text = "연결";
                File_InFo_GridView.Enabled = false;//비활성화 시켰던 친구들 활성화
                progressBar1.Enabled = false;
                File_Upload_Button.Enabled = false;
                Find_FilePath_Button.Enabled = false;
                Upload_FilePath.Enabled = false;

                IP_Address_Input.Enabled = true;//활성화 돼있던 친구들 비활성화
                Port.Enabled = true;
                Account_ID.Enabled = true;
                Password.Enabled = true;
                Download_Dir_Path.Enabled = true;
            }
        }

        //폴더 위치 찾아서 폴더 경로 텍스트박스에 집어넣는다.
        private void Get_Dir_Path_Click(object sender, EventArgs e)
        {
            if (FTP_Client_folderBrowser.ShowDialog() == DialogResult.OK)
               Download_Dir_Path.Text = FTP_Client_folderBrowser.SelectedPath;
            
        }

        //그..DataGridBox의 각 원소의 그 다운로드 버튼을 클릭하면 해당 버튼의 열에 맞는 파일을 다운로드 한다.파일을 업로드 한다. 프로세스바에 현재 진행상황을 띄운다.
        //근데 특정 셀 하나를 클릭하는게 CellContentClick이 맞나? 함 구글신의 힘을 빌어야할 것 같다.
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        //파일을 업로드 한다. 프로세스바에 현재 진행상황을 띄운다.
        private void File_Upload_Button_Click(object sender, EventArgs e)
        {

        }

    }
}
