namespace Primora.Domain.Entities;

internal static class Validacao
{
    public static Guid Identificador(Guid valor, string nome) => valor != Guid.Empty
        ? valor : throw new ArgumentException("O identificador é obrigatório.", nome);
    public static string Texto(string valor, string nome) => !string.IsNullOrWhiteSpace(valor)
        ? valor : throw new ArgumentException("O texto é obrigatório.", nome);
    public static int NaoNegativo(int valor, string nome) => valor >= 0
        ? valor : throw new ArgumentOutOfRangeException(nome);
    public static decimal NaoNegativo(decimal valor, string nome) => valor >= 0
        ? valor : throw new ArgumentOutOfRangeException(nome);
    public static int Ordem(int valor, string nome) => valor > 0
        ? valor : throw new ArgumentOutOfRangeException(nome);
    public static int Posicao(int valor, string nome) => valor is >= 1 and <= 30
        ? valor : throw new ArgumentOutOfRangeException(nome);
    public static int Score(int valor, string nome) => valor is >= 0 and <= 100
        ? valor : throw new ArgumentOutOfRangeException(nome);
    public static decimal Proporcao(decimal valor, string nome) => valor is >= 0 and <= 1
        ? valor : throw new ArgumentOutOfRangeException(nome);
    public static DateTime SemFuso(DateTime valor, string nome) => valor.Kind == DateTimeKind.Unspecified
        ? valor : throw new ArgumentException("O evento deve preservar o horário sem fuso informado na origem.", nome);
    public static DateTimeOffset Utc(DateTimeOffset valor, string nome) => valor.Offset == TimeSpan.Zero
        ? valor : throw new ArgumentException("O instante de importação deve estar em UTC.", nome);
    public static string Hash(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Length != 64 || !valor.All(char.IsAsciiHexDigit))
            throw new ArgumentException("Informe um hash SHA-256 hexadecimal válido.", nameof(valor));
        return valor.ToLowerInvariant();
    }
}
