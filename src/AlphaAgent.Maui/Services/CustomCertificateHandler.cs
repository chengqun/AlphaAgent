using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace AlphaAgent.Maui.Services;

/// <summary>
/// 从内嵌的 rootCA.crt 加载自签名 CA 证书，解决 Android/iOS 连接自签名服务器的问题。
/// 统一全平台证书信任，用户无需手动安装证书。
/// </summary>
public static class CustomCertificateHandler
{
    private static X509Certificate2Collection? _trustedCerts;

    /// <summary>
    /// 从 MAUI Raw 资源加载内嵌的 rootCA.crt 证书
    /// </summary>
    public static void Initialize()
    {
        try
        {
            _trustedCerts = new X509Certificate2Collection();

            using var stream = FileSystem.OpenAppPackageFileAsync("rootCA.crt").GetAwaiter().GetResult();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var certData = ms.ToArray();

            var cert = X509CertificateLoader.LoadCertificate(certData);
            _trustedCerts.Add(cert);

            System.Diagnostics.Debug.WriteLine($"[CustomCertificateHandler] 已加载内嵌 CA 证书: {cert.Subject}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomCertificateHandler] 初始化失败: {ex.Message}");
            _trustedCerts = null;
        }
    }

    /// <summary>
    /// 创建信任内嵌 CA 证书的 HttpMessageHandler，用于 HttpClient 和 SignalR
    /// </summary>
    public static HttpMessageHandler CreateHandler()
    {
        var handler = new SocketsHttpHandler();

        if (_trustedCerts != null && _trustedCerts.Count > 0)
        {
            handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        return true;

                    // 用内嵌 CA 证书验证服务器证书链
                    if (certificate != null && chain != null)
                    {
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.AddRange(_trustedCerts);
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                        using var cert2 = new X509Certificate2(certificate);
                        return chain.Build(cert2);
                    }

                    return false;
                }
            };
        }

        return handler;
    }
}
