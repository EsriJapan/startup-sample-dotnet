using Esri.ArcGISRuntime.Security;
using System.Threading.Tasks;

namespace MyMapApp
{
    internal class PortalSecurity
    {
        //ArcGIS Online REST の URL
        private const string PORTAL_URL = "https://www.arcgis.com/sharing/rest";

        //クライアントID（登録したアプリケーションのクライアントIDを指定します）
        private const string CLIENT_ID = "************************";

        //クライアント シークレット（登録したアプリケーションのクライアントシークレットを指定します）
        private const string CLIENT_SECRET = "************************";

        //リダイレクトURL
        private const string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// ArcGIS Online ログイン時に実行されるメソッド
        /// </summary>
        public static async Task<Credential> Challenge(CredentialRequestInfo arg)
        {
            //サーバー情報を取得
            var serverInfo = IdentityManager.Current.FindServerInfo(PORTAL_URL);

            //サーバ情報が登録されていない場合は新規に作成
            if (serverInfo == null)
            {
                serverInfo = new ServerInfo()
                {
                    ServerUri = PORTAL_URL,
                    OAuthClientInfo = new OAuthClientInfo()
                    {
                        ClientId = CLIENT_ID,
                        ClientSecret = CLIENT_SECRET,
                        RedirectUri = REDIRECT_URI,
                    },

                    //認証情報を指定（アプリケーション ログイン）
                    TokenAuthenticationType = TokenAuthenticationType.OAuthClientCredentials,
                };

                //サーバー情報を登録
                IdentityManager.Current.RegisterServer(serverInfo);
            }

            //認証情報を生成
            return await IdentityManager.Current.GenerateCredentialAsync(PORTAL_URL);
        }
    }
}
