using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;

/// <summary>
/// Provides CURL-like functionalities in T-SQL code.
/// </summary>
public partial class Curl
{
    readonly static System.Text.Encoding WINDOWS1251 = Encoding.GetEncoding(1251);
    readonly static System.Text.Encoding UTF8 = Encoding.UTF8;
    [SqlFunction]
    [return: SqlFacet(MaxSize = -1)]
    public static SqlChars Get(SqlChars H, SqlChars url)
    {
        var client = new WebClient();
        client.Encoding = System.Text.Encoding.UTF8;
        AddHeader(H, client);
        return new SqlChars(client.DownloadString(Uri.EscapeUriString(url.ToSqlString().Value)).ToCharArray());
    }

    [SqlFunction]
    [return: SqlFacet(MaxSize = -1)]
    public static SqlChars GetSSL(SqlChars H, SqlChars url)
    {
        Uri address = new Uri(url.ToSqlString().Value);
        if (address.Scheme.ToLower().Contains("https"))
        {            
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(address);
        request.Method = "GET";
        AddHeader2(H, request);

        Uri proxyUri;
        if (!HttpWebRequest.GetSystemWebProxy().IsBypassed(address))
        {
            proxyUri = HttpWebRequest.GetSystemWebProxy().GetProxy(address);
            request.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            request.Proxy = new WebProxy(proxyUri);
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
        }

        String result = String.Empty;
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        {
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            result = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
        }
        return new SqlChars(result);
    }

  [SqlProcedure]
   public static void GetRex(SqlChars H, SqlChars url, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
  {
    SqlChars result = new SqlChars("");
    httpStatusCode = 200;
    httpStatusDescription = "Success";
    response = result.ToSqlString();
    try
    {
      result = Get(H, url);
    }
    catch (WebException wex)
    {
      using (Stream requestStream = wex.Response.GetResponseStream())
      {
        byte[] bytes = new byte[requestStream.Length];
        int numBytesToRead = (int)requestStream.Length;
        int numBytesRead = 0;

        if (requestStream.CanRead)
        {
          while ((numBytesToRead > 0) && (numBytesToRead > 0))
          {
            int n = requestStream.Read(bytes, numBytesRead, numBytesToRead);
            if (n == 0)
              break;
            numBytesRead += n;
            numBytesToRead -= n;
          }
          numBytesToRead = bytes.Length;
          string responsefromServer = System.Text.Encoding.UTF8.GetString(bytes);

          result = new SqlChars(responsefromServer.ToCharArray());
          httpStatusCode = (int)((HttpWebResponse)wex.Response).StatusCode;
          httpStatusDescription = ((HttpWebResponse)wex.Response).StatusDescription;
        }
      }
    }    
    if (result != null)
      response = result.ToSqlString();
  }
   [SqlProcedure]
   public static void GetRexSSL(SqlChars H, SqlChars url, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
    {
        SqlChars result = new SqlChars("");
        httpStatusCode = 200;
        httpStatusDescription = "Success";
        response = result.ToSqlString();
        try
        {
            result = GetSSL(H, url);
        }
        catch (WebException wex)
        {
            using (Stream requestStream = wex.Response.GetResponseStream())
            {
                byte[] bytes = new byte[requestStream.Length];
                int numBytesToRead = (int)requestStream.Length;
                int numBytesRead = 0;

                if (requestStream.CanRead)
                {
                    while ((numBytesToRead > 0) && (numBytesToRead > 0))
                    {
                        int n = requestStream.Read(bytes, numBytesRead, numBytesToRead);
                        if (n == 0)
                            break;
                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;
                    string responsefromServer = System.Text.Encoding.UTF8.GetString(bytes);

                    result = new SqlChars(responsefromServer.ToCharArray());
                    httpStatusCode = (int)((HttpWebResponse)wex.Response).StatusCode;
                    httpStatusDescription = ((HttpWebResponse)wex.Response).StatusDescription;
                }
            }
        }
        if (result != null)
            response = result.ToSqlString();
    }

    [SqlProcedure]
    public static void PostEx(SqlChars H, SqlChars d, SqlChars url, out SqlString response )
    {
      SendEx(H, d, url, "POST", out response);
    }

  [SqlProcedure]
  public static void PostREx(SqlChars H, SqlChars d, SqlChars url, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
  {
    SendWithStatus(H, d, url, "POST", out response, out httpStatusCode, out httpStatusDescription);
  }

  [SqlProcedure]
    public static void PutEx(SqlChars H, SqlChars d, SqlChars url, out SqlString response)
    {
      SendEx(H, d, url, "PUT", out response);
    }
  [SqlProcedure]
  public static void PutREx(SqlChars H, SqlChars d, SqlChars url, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
  {
    SendWithStatus(H, d, url, "PUT", out response, out httpStatusCode, out httpStatusDescription);
  }

  [SqlProcedure]
  public static void PatchEx(SqlChars H, SqlChars d, SqlChars url, out SqlString response)
  {
    SendEx(H, d, url, "PATCH", out response);
  }

  [SqlProcedure]
  public static void PatchREx(SqlChars H, SqlChars d, SqlChars url, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
  {
    SendWithStatus(H, d, url, "PATCH", out response, out httpStatusCode, out httpStatusDescription);
  }

  [SqlProcedure]
  public static void DeleteEx(SqlChars H, SqlChars d, SqlChars url, out SqlString response)
  {
    SendEx(H, d, url, "DELETE", out response);
  }

  [SqlProcedure]
  public static void DeleteREx(SqlChars H, SqlChars d, SqlChars url, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
  {
    SendWithStatus(H, d, url, "DELETE", out response, out httpStatusCode, out httpStatusDescription);
  }

  [SqlProcedure]
  public static void Post(SqlChars H, SqlChars d, SqlChars url)
    {
        SqlString response;
        PostEx(H, d, url, out response);
        SqlContext.Pipe.Send("Request is executed. " + response.ToString());
    }
    
  [SqlProcedure]
  public static void PostWithRetry(SqlChars H, SqlChars d, SqlChars url)
    {
        var client = new WebClient();
        AddHeader(H, client);
        if (d.IsNull)
            throw new ArgumentException("You must specify data that will be sent to the endpoint", "@d");
        int i = RETRY_COUNT;
        string response = "";
        do try
            {
                response =
                        client.UploadString(
                            Uri.EscapeUriString(url.ToSqlString().Value),
                            d.ToSqlString().Value
                            );
                i = -1;
                break;
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send("Error:\t" + ex.Message + ". Waiting " + DELAY_ON_ERROR + "ms.");
                i--;
                Thread.Sleep(DELAY_ON_ERROR);
            }
        while (i > 0);
        if(i==-1)
            SqlContext.Pipe.Send("Request is executed." + response);
    }

  [SqlProcedure]
  public static void PostWithRetryEx(SqlChars H, SqlChars d, SqlChars url, out SqlString response)
  {
    response = "";
    var client = new WebClient();
    AddHeader(H, client);
    if (d.IsNull)
      throw new ArgumentException("You must specify data that will be sent to the endpoint", "@d");
    int i = RETRY_COUNT;
    string resp = "";
    do try
      {
        resp =
                client.UploadString(
                    Uri.EscapeUriString(url.ToSqlString().Value),
                    d.ToSqlString().Value
                    );
        i = -1;
        break;
      }
      catch (Exception ex)
      {
        SqlContext.Pipe.Send("Error:\t" + ex.Message + ". Waiting " + DELAY_ON_ERROR + "ms.");
        i--;
        Thread.Sleep(DELAY_ON_ERROR);
      }
    while (i > 0);
    if (i == -1)
    {
      SqlContext.Pipe.Send("Request is executed." + resp);
      response = resp;
    }
  }


  static readonly int RETRY_COUNT = 3;
  static readonly int DELAY_ON_ERROR = 50;

  static string ConvertWin1251ToUTF8(string inString)
  {
    return UTF8.GetString(WINDOWS1251.GetBytes(inString));
  }
  private static void AddHeader(SqlChars H, WebClient client)
    {
        if (!H.IsNull)
        {
            var header = H.ToSqlString().Value;
            if (!string.IsNullOrWhiteSpace(header))
            {
                foreach (var item in header.Split('|'))
                {
                    client.Headers.Add(item);
                }
            }
        }
    }
  private static void AddHeader2(SqlChars H, HttpWebRequest client)
  {
    if (!H.IsNull)
    {
      var header = H.ToSqlString().Value;
      if (!string.IsNullOrWhiteSpace(header))
      {
        string head_str;
        foreach (var item in header.Split('|'))
        {
          head_str = item;
          if (item.Contains("Content-Type"))
          {
            int i = 0;
            foreach (var item2 in item.Split(':'))
            {
              i++;
              if (i == 2)
                head_str = item2.Trim();
            }
            client.ContentType = head_str;
          }
          else
          {
            client.Headers.Add(item);
          }
          
        }
      }
    }
  }
  private static void SendEx(SqlChars H, SqlChars d, SqlChars url, string method, out SqlString response)
  {
    Uri address = new Uri(url.ToSqlString().Value);
    if (address.Scheme.ToLower().Contains("https"))
    {
       SqlContext.Pipe.Send("Secure Http request.");
       ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
       ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
       ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
    }
    using (var client = new WebClient())
    {
      AddHeader(H, client);
      client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; Windows NT 10.0; Win64; x64; ru-RU; rv:1.9.2.6) Gecko/20100625 Firefox/3.6.6 (.NET CLR 3.5.30729)";
      //client.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.110 Safari/537.36";

      if (d.IsNull)
      {
        throw new ArgumentException("You must specify data that will be sent to the endpoint", "@d");
      }
      //var proxy = new WebProxy();
      //proxy.Address = new Uri("http://gw:8080/array.dll?Get.Routing.Script");
      Uri proxyUri;
      if (!WebRequest.GetSystemWebProxy().IsBypassed(address))
      {
        proxyUri = WebRequest.GetSystemWebProxy().GetProxy(address);
        client.Credentials = System.Net.CredentialCache.DefaultCredentials;
        client.Proxy = new WebProxy(proxyUri);
        client.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
      }
      //client.Proxy.Credentials = new System.Net.NetworkCredential(@"eshenko_sergey", @"23Qwerty ","gamma");
      //client.Credentials = new System.Net.NetworkCredential(@"eshenko_sergey", @"23Qwerty ", "gamma");
      client.Encoding = System.Text.Encoding.UTF8;
      response = client.UploadString(
                              Uri.EscapeUriString(url.ToSqlString().Value),
                              method.ToUpper(),
                              d.ToSqlString().Value
                              );        
    }
  }

  private static void SendWithStatus(SqlChars H, SqlChars d, SqlChars url, string method, out SqlString response, out int httpStatusCode, out SqlString httpStatusDescription)
  {
    httpStatusCode = 200;
    httpStatusDescription = "Success";
    response = "";
    try
    {
      SendEx(H, d, url, method, out response);
    }
    catch (WebException wex)
    {
      httpStatusCode = (int)wex.Status;
      httpStatusDescription = wex.StackTrace;
      if (wex.Response != null)
      {
        using (Stream requestStream = wex.Response.GetResponseStream())
        {
          byte[] bytes = new byte[requestStream.Length];
          int numBytesToRead = (int)requestStream.Length;
          int numBytesRead = 0;

          if (requestStream.CanRead)
          {
            while ((numBytesToRead > 0) && (numBytesToRead > 0))
            {
              int n = requestStream.Read(bytes, numBytesRead, numBytesToRead);
              if (n == 0)
                break;
              numBytesRead += n;
              numBytesToRead -= n;
            }
            numBytesToRead = bytes.Length;
            string responsefromServer = System.Text.Encoding.UTF8.GetString(bytes);

            response = responsefromServer;

            httpStatusCode = (int)((HttpWebResponse)wex.Response).StatusCode;
            httpStatusDescription = ((HttpWebResponse)wex.Response).StatusDescription;

          }
        }
      }
    }
    catch (Exception ex)
    {
      response = ex.Message;
    }
  }

  private static void SendSEx(SqlChars H, SqlChars d, SqlChars url, string method, out SqlString response)
    {
        using (var client = new WebClient())
        {
            AddHeader(H, client);
            if (d.IsNull)
                throw new ArgumentException("You must specify data that will be sent to the endpoint", "@d");

            ServicePointManager.ServerCertificateValidationCallback += 
                delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                   return true;
                };
            response = client.UploadString( Uri.EscapeUriString(url.ToSqlString().Value),
                                            method.ToUpper(),
                                            d.ToSqlString().Value);
        }
    }

    private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
    {
        // If the certificate is a valid, signed certificate, return true.
        return true;
        //if (error == System.Net.Security.SslPolicyErrors.None)
        //{           
        //    return true;
        //}
        //return false;
    }
};