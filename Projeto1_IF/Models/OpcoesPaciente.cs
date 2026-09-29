namespace Projeto1_IF.Models;

// Listas fixas usadas nos dropdowns do formulário de paciente
public static class OpcoesPaciente
{
    // Classificação de cor/raça do IBGE
    public static readonly Dictionary<int, string> Etnias = new()
    {
        { 1, "Branca" },
        { 2, "Preta" },
        { 3, "Parda" },
        { 4, "Amarela" },
        { 5, "Indígena" }
    };

    public static readonly Dictionary<string, string> Sexos = new()
    {
        { "M", "Masculino" },
        { "F", "Feminino" }
    };
}