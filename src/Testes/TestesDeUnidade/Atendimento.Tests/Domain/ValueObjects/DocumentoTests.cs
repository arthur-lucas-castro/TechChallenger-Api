using Atendimento.Domain.ValueObjects;

namespace Atendimento.Tests.Domain.ValueObjects;

public class DocumentoTests
{
    // CPF válido usado nos testes: 529.982.247-25
    private const string CpfValido = "52998224725";
    // CNPJ válido: 11.222.333/0001-81
    private const string CnpjValido = "11222333000181";

    [Fact]
    public void CpfValido_DeveCriarDocumento()
    {
        // Arrange & Act
        var doc = new Documento(CpfValido);

        // Assert
        Assert.Equal(CpfValido, doc.Numero);
    }

    [Fact]
    public void CpfComFormatacao_DeveCriarDocumento()
    {
        // Arrange
        var cpfFormatado = "529.982.247-25";

        // Act
        var doc = new Documento(cpfFormatado);

        // Assert — pontos e hífen devem ser ignorados
        Assert.Equal(CpfValido, doc.Numero);
    }

    [Fact]
    public void CpfDigitosVerificadoresInvalidos_DeveLancarArgumentException()
    {
        // Arrange — último dígito alterado de 5 para 6
        var cpfInvalido = "52998224726";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(cpfInvalido));
    }

    [Fact]
    public void CpfComTodosDigitosIguais_DeveLancarArgumentException()
    {
        // Arrange — sequência homogênea é explicitamente rejeitada
        var cpfInvalido = "11111111111";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(cpfInvalido));
    }

    [Fact]
    public void CnpjValido_DeveCriarDocumento()
    {
        // Arrange & Act
        var doc = new Documento(CnpjValido);

        // Assert
        Assert.Equal(CnpjValido, doc.Numero);
    }

    [Fact]
    public void CnpjComFormatacao_DeveCriarDocumento()
    {
        // Arrange
        var cnpjFormatado = "11.222.333/0001-81";

        // Act
        var doc = new Documento(cnpjFormatado);

        // Assert — formatação deve ser descartada
        Assert.Equal(CnpjValido, doc.Numero);
    }

    [Fact]
    public void CnpjDigitosVerificadoresInvalidos_DeveLancarArgumentException()
    {
        // Arrange — último dígito alterado de 1 para 2
        var cnpjInvalido = "11222333000182";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(cnpjInvalido));
    }

    [Fact]
    public void CnpjComTodosDigitosIguais_DeveLancarArgumentException()
    {
        // Arrange — sequência homogênea é explicitamente rejeitada
        var cnpjInvalido = "11111111111111";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(cnpjInvalido));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void DocumentoSemDigitosValidos_DeveLancarArgumentException(string entrada)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(entrada));
    }

    [Fact]
    public void DocumentoComTamanhoErrado_DeveLancarArgumentException()
    {
        // Arrange — 12 dígitos: nem CPF (11) nem CNPJ (14)
        var docInvalido = "123456789012";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(docInvalido));
    }

    [Fact]
    public void EhCpf_RetornaTrue_ParaCpf()
    {
        // Arrange
        var doc = new Documento(CpfValido);

        // Act & Assert
        Assert.True(doc.EhCpf);
        Assert.False(doc.EhCnpj);
    }

    [Fact]
    public void EhCnpj_RetornaTrue_ParaCnpj()
    {
        // Arrange
        var doc = new Documento(CnpjValido);

        // Act & Assert
        Assert.True(doc.EhCnpj);
        Assert.False(doc.EhCpf);
    }

    [Fact]
    public void ConversaoImplicita_StringParaDocumento()
    {
        // Arrange
        string cpf = CpfValido;

        // Act
        Documento doc = cpf;

        // Assert
        Assert.Equal(CpfValido, doc.Numero);
    }
}
