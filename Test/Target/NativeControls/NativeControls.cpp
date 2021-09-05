
// NativeControls.cpp : アプリケーションのクラス動作を定義します。
//

#include "pch.h"
#include "framework.h"
#include "afxwinappex.h"
#include "afxdialogex.h"
#include "NativeControls.h"
#include "MainFrm.h"

#include "ChildFrm.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CNativeControlsApp

BEGIN_MESSAGE_MAP(CNativeControlsApp, CWinApp)
	ON_COMMAND(ID_APP_ABOUT, &CNativeControlsApp::OnAppAbout)
	ON_COMMAND(ID_FILE_NEW, &CNativeControlsApp::OnFileNew)
END_MESSAGE_MAP()


// CNativeControlsApp の構築

CNativeControlsApp::CNativeControlsApp() noexcept
{

	// TODO: 下のアプリケーション ID 文字列を一意の ID 文字列で置換します。推奨される
	// 文字列の形式は CompanyName.ProductName.SubProduct.VersionInformation です
	SetAppID(_T("NativeControls.AppID.NoVersion"));

	// TODO: この位置に構築用コードを追加してください。
	// ここに InitInstance 中の重要な初期化処理をすべて記述してください。
}

// 唯一の CNativeControlsApp オブジェクト

CNativeControlsApp theApp;


// CNativeControlsApp の初期化

BOOL CNativeControlsApp::InitInstance()
{
	CWinApp::InitInstance();


	EnableTaskbarInteraction(FALSE);

	// RichEdit コントロールを使用するには AfxInitRichEdit2() が必要です
	// AfxInitRichEdit2();

	// 標準初期化
	// これらの機能を使わずに最終的な実行可能ファイルの
	// サイズを縮小したい場合は、以下から不要な初期化
	// ルーチンを削除してください。
	// 設定が格納されているレジストリ キーを変更します。
	// TODO: 会社名または組織名などの適切な文字列に
	// この文字列を変更してください。
	SetRegistryKey(_T("アプリケーション ウィザードで生成されたローカル アプリケーション"));


	// メイン ウィンドウを作成するとき、このコードは新しいフレーム ウィンドウ オブジェクトを作成し、
	// それをアプリケーションのメイン ウィンドウにセットします
	CMDIFrameWnd* pFrame = new CMainFrame;
	if (!pFrame)
		return FALSE;
	m_pMainWnd = pFrame;
	// メイン MDI フレーム ウィンドウを作成します。
	if (!pFrame->LoadFrame(IDR_MAINFRAME))
		return FALSE;
	// 共通の MDI メニューとアクセラレータ テーブルを読み込みます。
	//TODO: 追加のメンバー変数を加えて、アプリケーションが必要とする
	//	追加のメニュータイプのために呼び出しを読み込んでください。
	HINSTANCE hInst = AfxGetResourceHandle();
	m_hMDIMenu  = ::LoadMenu(hInst, MAKEINTRESOURCE(IDR_NativeControlsTYPE));
	m_hMDIAccel = ::LoadAccelerators(hInst, MAKEINTRESOURCE(IDR_NativeControlsTYPE));




	// メイン ウィンドウが初期化されたので、表示と更新を行います。
	pFrame->ShowWindow(m_nCmdShow);
	pFrame->UpdateWindow();

	return TRUE;
}

int CNativeControlsApp::ExitInstance()
{
	//TODO: 追加したリソースがある場合にはそれらも処理してください
	if (m_hMDIMenu != nullptr)
		FreeResource(m_hMDIMenu);
	if (m_hMDIAccel != nullptr)
		FreeResource(m_hMDIAccel);

	return CWinApp::ExitInstance();
}

// CNativeControlsApp メッセージ ハンドラー

void CNativeControlsApp::OnFileNew()
{
	CMainFrame* pFrame = STATIC_DOWNCAST(CMainFrame, m_pMainWnd);
	// 新しい MDI 子ウィンドウを作成します
	pFrame->CreateNewChild(
		RUNTIME_CLASS(CChildFrame), IDR_NativeControlsTYPE, m_hMDIMenu, m_hMDIAccel);
}

// アプリケーションのバージョン情報に使われる CAboutDlg ダイアログ

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg() noexcept;

// ダイアログ データ
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_ABOUTBOX };
#endif

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV サポート

// 実装
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() noexcept : CDialogEx(IDD_ABOUTBOX)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()

// ダイアログを実行するためのアプリケーション コマンド
void CNativeControlsApp::OnAppAbout()
{
	CAboutDlg aboutDlg;
	aboutDlg.DoModal();
}

// CNativeControlsApp メッセージ ハンドラー



