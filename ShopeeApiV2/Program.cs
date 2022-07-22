
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

string host = string.Empty;
bool confirmEnviroment = true;
while (confirmEnviroment)
{
    Console.Write("使用Shopee正式環境?(Y/N):");
    var isOfficial = Console.ReadLine();
    var integratedIsOfficial = isOfficial.Trim().ToUpper();
    switch (integratedIsOfficial)
    {
        case "Y":
            host = "https://partner.shopeemobile.com";
            confirmEnviroment = false;
            break;
        case "N":
            host = "https://partner.test-stable.shopeemobile.com";
            confirmEnviroment = false;
            break;
    }
}

Console.Write("請輸入partnerId:");
var partner_id = Console.ReadLine();
Console.Write("請輸入App Key:");
var appKey = Console.ReadLine();
Console.Write("請輸入授權完成後導向網址:");
var redirectWebsite = Console.ReadLine();

var timestamp = int.Parse(UnixTime(DateTime.UtcNow).ToString());
var codeSign = Sign("/api/v2/shop/auth_partner", partner_id!, timestamp.ToString(), appKey!);
string codeRequestURL = host + $"/api/v2/shop/auth_partner?partner_id={partner_id}&redirect={redirectWebsite}&timestamp={timestamp}&sign={codeSign}";

Console.WriteLine($"授權網址:");
Console.WriteLine($"{codeRequestURL}" + Environment.NewLine);
Console.WriteLine("請於3分鐘內登入並授權，授權完畢跳轉頁面後請於10分鐘內將回傳網址交給工程師。" + Environment.NewLine);
Console.Write("請輸入回傳網址:");
string responseUrl = string.Empty;
confirmEnviroment = true;
while (confirmEnviroment)
{
    responseUrl = Console.ReadLine()!;
    if(responseUrl.Length > 0)
    {
        confirmEnviroment=false;
    }
}
var query = GetUrlQuery(responseUrl!);
KeyValuePair<string, string> idInfo;
timestamp = int.Parse(UnixTime(DateTime.UtcNow).ToString());

var request = new AccessTokenRequest
{
    code = query[cst.Code],
    partner_id = int.Parse(partner_id!)
};

if (query.ContainsKey(cst.ShopId))
{
    request.shop_id = int.Parse(query[cst.ShopId]);
    idInfo = new KeyValuePair<string, string>(cst.ShopId, query[cst.ShopId]);
}
else if (query.ContainsKey(cst.MainAccountId))
{
    idInfo = new KeyValuePair<string, string>(cst.MainAccountId, query[cst.MainAccountId]);
}
else
{
    throw new Exception("網址參數不完全");
}
var accessTokenSign = Sign("/api/v2/auth/token/get", partner_id!, timestamp.ToString(), appKey!);
string AccessTokenRequestURL = host + $"/api/v2/auth/token/get?partner_id={partner_id}&timestamp={timestamp}&sign={accessTokenSign}";

HttpClient _client = new HttpClient();
var content = new StringContent(JsonConvert.SerializeObject(request), null, Application.Json);
var response = await _client.PostAsync(AccessTokenRequestURL, content);

var result = JsonConvert.DeserializeObject<AccessTokenResponse>(response.Content.ReadAsStringAsync().Result)!;

if(result.error == String.Empty)
{
    Console.WriteLine($"AccessToken:{result.access_token}");
    Console.WriteLine($"RefreshToken:{result.refresh_token}");
    Console.WriteLine($"{idInfo.Key}:{idInfo.Value}");
}
else
{
    Console.WriteLine($"error:{result.error}");
    Console.WriteLine($"message:{result.message}");
}

while (true)
{
    Console.ReadLine();
}



static string Sign(string apiPathWithoutHost, string partnerId, string timestamp, string shopeePrivateKey, string other = "")
{

    string signatureBasedString = partnerId + apiPathWithoutHost + timestamp + other;


    UTF8Encoding encoding = new UTF8Encoding();
    byte[] keyByte = encoding.GetBytes(shopeePrivateKey);
    HMACSHA256 hmacsha1 = new HMACSHA256(keyByte);

    byte[] messageBytes = encoding.GetBytes(signatureBasedString);
    byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);

    string signature = HexStringFromBytes(hashmessage);

    return signature;
}

static string HexStringFromBytes(byte[] bytes)
{
    var sb = new System.Text.StringBuilder();
    foreach (byte b in bytes)
    {
        var hex = b.ToString("x2");
        sb.Append(hex);
    }
    return sb.ToString();
}

static long UnixTime(DateTime tt)
{
    long epochTicks = new DateTime(1970, 1, 1).Ticks;
    long unixTime = ((tt.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
    return unixTime;
}
static Dictionary<string, string> GetUrlQuery(string url)
{
    Uri uri = new Uri(url.Trim());
    string queryString = uri.Query;
    Dictionary<string, string> result = new Dictionary<string, string>();
    StringBuilder sb = new StringBuilder();
    string name = string.Empty;
    for (int i = 0; i < queryString.Length; i++)
    {
        if (queryString[i] == '?')
        {
            continue;
        }
        else if (queryString[i] == '=')
        {
            name = sb.ToString();
            sb.Clear();
            continue;
        }
        else if (queryString[i] == '&')
        {
            result.Add(name, sb.ToString());
            name = string.Empty;
            sb.Clear();
            continue;
        }
        else
        {
            sb.Append(queryString[i]);

            if (i == queryString.Length - 1)
            {
                result.Add(name, sb.ToString());
            }
        }
    }
    return result;
}

static class cst
{
    public static string Code { get { return "code"; } }
    public static string ShopId { get { return "shop_id"; } }
    public static string MainAccountId { get { return "main_account_id"; } }
}

class AccessTokenRequest
{
    public string code { get; set; }
    public int partner_id { get; set; }
    public int? shop_id { get; set; }
    public int? main_account_id { get; set; }
}

class AccessTokenResponse
{
    public string access_token { get; set; }
    public string refresh_token { get; set; }
    public string error { get; set; }
    public string message { get; set; }
}