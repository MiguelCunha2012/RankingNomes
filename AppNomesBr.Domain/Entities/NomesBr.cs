using SQLite;

namespace AppNomesBr.Domain.Entities
{
    [Table("NomesBr")]
    public class NomesBr
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public int Id { get; set; }

        [NotNull, Unique]
        [Column("Nome")]
        public string Nome { get; set; } = string.Empty;

        [NotNull]
        [Column("Periodo")]
        public string Periodo { get; set; } = string.Empty;

        [NotNull]
        [Column("Frequencia")]
        public long Frequencia { get; set; }

        [NotNull]
        [Column("Ranking")]
        public int Ranking { get; set; }

        [Column("Sexo")]
        public string? Sexo { get; set; } // "M" | "F"
    }
}
