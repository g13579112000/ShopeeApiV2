using System.Security.Cryptography;
using System.Text;


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

var timestamp = UnixTime(DateTime.UtcNow).ToString();
var sign = Sign("/api/v2/shop/auth_partner", partner_id!, timestamp, appKey!);

string requestCommonParameters = $"?partner_id={partner_id}&redirect={redirectWebsite}&timestamp={timestamp}&sign={sign}";
string requestURL = host + "/api/v2/shop/auth_partner" + requestCommonParameters;
Console.WriteLine($"授權網址:");
Console.WriteLine($"{requestURL}" + Environment.NewLine);
Console.WriteLine("請於3分鐘內登入並授權，授權完畢跳轉頁面後請於5分鐘內將回傳網址交給工程師。");

Console.ReadLine();

static string Sign(string apiPathWithoutHost, string partnerId, string timestamp, string shopeePrivateKey)
{

    string signatureBasedString = partnerId + apiPathWithoutHost + timestamp;


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