using Npgsql;
using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SkillBridge.Helpers
{
    // Npgsql 4.1 validates TLS certificates but cannot load a custom root CA
    // from its connection string. Supabase's pooler uses a private root CA.
    public sealed class SupabaseConnectionFactory : IDbConnectionFactory
    {
        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            var settings = new NpgsqlConnectionStringBuilder(nameOrConnectionString);
            if (settings.SslMode != SslMode.Require || settings.TrustServerCertificate)
                throw new InvalidOperationException("SKILLBRIDGE_DB_CONNECTION must use SSL Mode=Require without Trust Server Certificate.");

            var certificatePath = Environment.GetEnvironmentVariable("SKILLBRIDGE_DB_CA_CERT");
            if (string.IsNullOrWhiteSpace(certificatePath))
                throw new InvalidOperationException("Set SKILLBRIDGE_DB_CA_CERT to the path of the Supabase CA certificate.");

            // A relative path is resolved from the application root, which is
            // stable across Windows hosts even when their physical paths differ.
            if (!Path.IsPathRooted(certificatePath))
                certificatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, certificatePath);
            if (!File.Exists(certificatePath))
                throw new InvalidOperationException("The Supabase CA certificate configured by SKILLBRIDGE_DB_CA_CERT was not found.");

            var trustedRoot = new X509Certificate2(certificatePath);
            var connection = new NpgsqlConnection(nameOrConnectionString);
            connection.UserCertificateValidationCallback = (sender, certificate, chain, errors) =>
                IsTrustedSupabaseCertificate(certificate, chain, errors, trustedRoot);
            return connection;
        }

        private static bool IsTrustedSupabaseCertificate(
            X509Certificate certificate, X509Chain chain, SslPolicyErrors errors, X509Certificate2 trustedRoot)
        {
            if (certificate == null || chain == null || chain.ChainElements.Count == 0 ||
                (errors & (SslPolicyErrors.RemoteCertificateNameMismatch |
                           SslPolicyErrors.RemoteCertificateNotAvailable)) != 0)
                return false;

            foreach (var status in chain.ChainStatus)
            {
                if (status.Status != X509ChainStatusFlags.NoError &&
                    status.Status != X509ChainStatusFlags.UntrustedRoot)
                    return false;
            }

            var actualRoot = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            return string.Equals(actualRoot.Thumbprint, trustedRoot.Thumbprint, StringComparison.OrdinalIgnoreCase);
        }
    }
}
