namespace Barbearia.Configs
{
    /// <summary>
    /// O acesso publicado desta copia de demonstracao.
    ///
    /// Esta copia existe para ser aberta por quem nunca a viu, entao ela
    /// precisa de um acesso que funcione sem configuracao nenhuma -- e,
    /// se esta escrito na tela de entrada, tem de sair de um lugar so,
    /// senao a tela e o banco divergem no primeiro ajuste.
    ///
    /// Definir ADMIN_USUARIO e ADMIN_SENHA no ambiente substitui isto. No
    /// sistema de verdade nao ha acesso padrao nenhum: sem as duas
    /// variaveis ele sobe sem usuario, e o cadastro e manual.
    /// </summary>
    public static class Demonstracao
    {
        public const string Usuario = "admin";

        /// <summary>
        /// Atende a politica de senha do sistema (letra e numero, minimo
        /// de caracteres). Nao e segredo: esta impressa na tela de
        /// entrada e no README, de proposito.
        /// </summary>
        public const string Senha = "demo1234";
    }
}
