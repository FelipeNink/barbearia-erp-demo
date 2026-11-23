using Barbearia.Configs;
using MySqlConnector;

namespace Barbearia.Data
{
    /// <summary>
    /// Logo e cores da barbearia, escolhidas pelo administrador e vistas por
    /// todo mundo. Quem nao e administrador pode por as suas por cima, so no
    /// proprio navegador (ver marca no ui.js).
    /// </summary>
    public class MarcaDAO
    {
        private readonly Conexao _conexao;

        public MarcaDAO(Conexao conexao)
        {
            _conexao = conexao;
        }

        /// <summary>
        /// O que vale para todos. A imagem fica de fora: a pagina so precisa
        /// saber se ha logo e a versao dela, para montar o endereco.
        /// </summary>
        public record MarcaGlobal(string Primaria, string Secundaria, bool TemLogo, long Versao)
        {
            public string? UrlLogo => TemLogo ? $"/marca/logo?v={Versao}" : null;
        }

        public MarcaGlobal Ler()
        {
            try
            {
                using var conexao = _conexao.GetConnection();
                using var comando = new MySqlCommand(@"
                    SELECT primaria, secundaria, logo IS NOT NULL AS tem_logo, atualizado_em
                      FROM marca_sistema WHERE id = 1", conexao);
                using var leitor = comando.ExecuteReader();

                if (!leitor.Read()) return Padrao;

                return new MarcaGlobal(
                    DAOHelper.IsNull(leitor, "primaria") ? Paleta.Padrao : leitor.GetString("primaria"),
                    DAOHelper.IsNull(leitor, "secundaria") ? Paleta.Padrao : leitor.GetString("secundaria"),
                    Convert.ToInt32(leitor.GetValue(leitor.GetOrdinal("tem_logo"))) == 1,
                    leitor.GetDateTime("atualizado_em").Ticks);
            }
            catch (MySqlException)
            {
                // Sem banco a pagina ainda precisa abrir, com a marca padrao.
                return Padrao;
            }
        }

        private static readonly MarcaGlobal Padrao = new(Paleta.Padrao, Paleta.Padrao, false, 0);

        public (string Tipo, byte[] Conteudo)? LerLogo()
        {
            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(
                "SELECT logo_tipo, logo FROM marca_sistema WHERE id = 1 AND logo IS NOT NULL", conexao);
            using var leitor = comando.ExecuteReader();

            if (!leitor.Read()) return null;

            return (leitor.GetString("logo_tipo"), (byte[])leitor["logo"]);
        }

        public void SalvarCores(string primaria, string secundaria)
        {
            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(
                "UPDATE marca_sistema SET primaria = @p, secundaria = @s WHERE id = 1", conexao);

            comando.Parameters.AddWithValue("@p", primaria);
            comando.Parameters.AddWithValue("@s", secundaria);
            comando.ExecuteNonQuery();
        }

        /// <summary>Grava a logo; tipo e conteudo nulos tiram a logo.</summary>
        public void SalvarLogo(string? tipo, byte[]? conteudo)
        {
            using var conexao = _conexao.GetConnection();
            using var comando = new MySqlCommand(
                "UPDATE marca_sistema SET logo_tipo = @tipo, logo = @logo WHERE id = 1", conexao);

            comando.Parameters.AddWithValue("@tipo", tipo ?? (object)DBNull.Value);
            comando.Parameters.AddWithValue("@logo", conteudo ?? (object)DBNull.Value);
            comando.ExecuteNonQuery();
        }
    }
}
