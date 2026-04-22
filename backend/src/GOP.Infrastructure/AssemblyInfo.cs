using System.Runtime.CompilerServices;

// Permite que GOP.Infrastructure.Tests instancie tipos internal (UserRepository, RefreshTokenRepository, etc.)
[assembly: InternalsVisibleTo("GOP.Infrastructure.Tests")]
// NSubstitute usa Castle DynamicProxy para crear proxies de tipos internos
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
