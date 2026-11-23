using System.Globalization;
using System.Text;

namespace Barbearia.Configs
{
    /// <summary>
    /// As cores que a pessoa pode escolher para a marca, e o CSS que sai
    /// de cada escolha.
    ///
    /// Fonte unica: as bolinhas da tela de conta e as variaveis aplicadas
    /// na pagina saem daqui. Se a lista vivesse no JS e os tons no C#
    /// (ou o contrario), uma bolinha acabaria pintando uma cor diferente
    /// da que ela mostra.
    ///
    /// O padrao nao esta nesta lista de proposito: escolher "padrao" nao
    /// aplica tom nenhum, apaga o override e deixa o app.css valer. Os
    /// tons originais foram ajustados a mao e nenhuma formula os
    /// reproduz exatamente -- derivar o padrao daqui mudaria a cara do
    /// sistema sem ninguem pedir.
    /// </summary>
    public static class Paleta
    {
        public const string Padrao = "padrao";

        /// <summary>
        /// O que a bolinha do padrao mostra. Sao as cores que o app.css ja
        /// usa; escolher o padrao nao aplica estes valores, apaga o
        /// override para o proprio app.css valer.
        /// </summary>
        public const string PrimariaPadrao = "#f0651a";
        public const string SecundariaPadrao = "#14171c";

        public record Cor(string Chave, string Rotulo, string Hex);

        /// <summary>
        /// Onde hoje e laranja. O laranja nao esta na lista porque ja e a
        /// bolinha do padrao -- repeti-lo daria duas bolinhas iguais com
        /// comportamentos diferentes.
        /// </summary>
        public static readonly Cor[] Primarias =
        {
            new("preto",    "Preto",    "#14171c"),
            new("branco",   "Branco",   "#ffffff"),
            new("vermelho", "Vermelho", "#e23b2e"),
            new("ambar",    "Âmbar",    "#d99100"),
            new("verde",    "Verde",    "#16a34a"),
            new("ciano",    "Ciano",    "#0891b2"),
            new("azul",     "Azul",     "#2563eb"),
            new("roxo",     "Roxo",     "#7c3aed"),
            new("rosa",     "Rosa",     "#db2777")
        };

        /// <summary>
        /// Onde hoje e preto: o fundo da lateral, o quadrado do icone e as
        /// letras da marca no tema claro.
        ///
        /// Claras tambem. O que escreve por cima nao e branco cravado em
        /// lugar nenhum -- sai do --sobre-carvao, que esta neste arquivo e
        /// e decidido por contraste. Escolher areia ou branco vira menu de
        /// letra escura, sem ninguem precisar mexer em nada.
        /// </summary>
        public static readonly Cor[] Secundarias =
        {
            // escuras
            new("grafite",  "Grafite",     "#2b2f36"),
            new("chumbo",   "Chumbo",      "#444c59"),
            new("noite",    "Azul-noite",  "#11203a"),
            new("oceano",   "Azul-oceano", "#123a4d"),
            new("mata",     "Verde-mata",  "#10261c"),
            new("pinho",    "Verde-pinho", "#1c3b2a"),
            new("vinho",    "Vinho",       "#2a1116"),
            new("cafe",     "Café",        "#2b1f18"),
            new("ametista", "Roxo-escuro", "#1d1430"),
            new("tinta",    "Azul-tinta",  "#1b2a6b"),

            // claras
            new("areia",    "Areia",       "#e6ded1"),
            new("linho",    "Linho",       "#f1ece3"),
            new("nevoa",    "Névoa",       "#e7ebf1"),
            new("gelo",     "Azul-gelo",   "#dbe7f2"),
            new("menta",    "Menta",       "#dcece2"),
            new("rose",     "Rosé",        "#f2e2e5"),
            new("creme",    "Creme",       "#faf6ef"),
            new("branco",   "Branco",      "#ffffff")
        };

        /// <summary>
        /// O --fundo do tema claro. Serve para saber se a cor secundaria
        /// escolhida ainda da para ler escrita sobre a pagina.
        /// </summary>
        private static readonly Cor3 FundoClaro = Cor3.De("#f7f8fa");

        /// <summary>
        /// O --superficie de cada tema: o cartao onde os botoes pousam. E
        /// contra ele que a cor primaria precisa se destacar.
        /// </summary>
        private static readonly Cor3 SuperficieClara = Cor3.De("#ffffff");
        private static readonly Cor3 SuperficieEscura = Cor3.De("#1b1f26");

        /// <summary>
        /// Preto ou branco por cima desta cor, pelo contraste. A tela da
        /// conta usa para o ponto que marca a bolinha do padrao, que
        /// precisa aparecer sobre a amostra seja ela qual for.
        /// </summary>
        public static string TextoSobre(string hex) => Cor3.De(hex).TextoLegivel;

        public static Cor? PrimariaPor(string? chave) =>
            Primarias.FirstOrDefault(c => c.Chave == chave);

        public static Cor? SecundariaPor(string? chave) =>
            Secundarias.FirstOrDefault(c => c.Chave == chave);

        /// <summary>
        /// Monta o CSS do override. Devolve vazio quando as duas escolhas
        /// sao o padrao -- e o vazio que o JS usa para apagar a tag.
        ///
        /// Os seletores comecam com html:root para ganharem de :root e de
        /// [data-tema="escuro"] do app.css por especificidade, e nao pela
        /// ordem em que a tag entrou no documento.
        /// </summary>
        public static string Css(string? primaria, string? secundaria)
        {
            var p = PrimariaPor(primaria);
            var s = SecundariaPor(secundaria);

            if (p == null && s == null) return "";

            var claro = new StringBuilder();
            var escuro = new StringBuilder();

            if (p != null)
            {
                // A cor primaria vira fundo de botao, e botao que nao se
                // separa do cartao atras dele some. Branco sobre o cartao
                // branco do tema claro, e preto sobre o #1b1f26 do escuro,
                // sao exatamente esse caso: o ComContraste afasta o tom da
                // superficie ate dar para distinguir os dois.
                //
                // E por isso que Branco sai cinza no tema claro e branco de
                // verdade no escuro, e Preto faz o caminho inverso. Nao ha
                // como um botao branco aparecer sobre um cartao branco.
                var b = Cor3.De(p.Hex).ComContraste(SuperficieClara, 3);

                // Claro: os mesmos passos que separam #f0651a de #d35400,
                // #e67e22 e #e8b48a no app.css.
                claro.Append($"--marca:{b.Hex};");
                claro.Append($"--marca-escura:{b.Mover(-11, +12).Hex};");
                claro.Append($"--marca-clara:{b.Mover(+7, -8).Hex};");
                claro.Append($"--marca-fraca:{b.Mover(+21, -21).Hex};");
                claro.Append($"--anel-foco:0 0 0 3px {b.Rgba(0.18)};");
                claro.Append($"--sobre-marca:{b.TextoLegivel};");
                claro.Append($"--sidebar-1:{b.Mover(-4, 0).Hex};");
                claro.Append($"--sidebar-2:{b.Hex};");
                claro.Append($"--sidebar-3:{b.Mover(+3, 0).Hex};");

                // Escuro: o tom puro satura demais, entao clareia -- a
                // mesma razao que levou #f0651a a virar #ff7a33. Parte do
                // tom original, e nao do ja afastado do branco la em cima:
                // o que o tema claro precisou corrigir nao vale aqui.
                var e = Cor3.De(p.Hex).Mover(+8, +12).ComContraste(SuperficieEscura, 3);
                // Todos derivam do tom ja corrigido para o tema escuro, e
                // nao do tom do tema claro. Com Preto, o tom claro e o
                // proprio #14171c: o --marca-escura, que e a cor dos
                // links, ficaria preto sobre o fundo escuro.
                escuro.Append($"--marca:{e.Hex};");
                escuro.Append($"--marca-escura:{e.Mover(-9, 0).ComContraste(SuperficieEscura, 3).Hex};");
                escuro.Append($"--marca-clara:{e.Mover(+8, 0).Hex};");
                escuro.Append($"--marca-fraca:{e.Mover(-31, -42).Hex};");
                escuro.Append($"--anel-foco:0 0 0 3px {e.Rgba(0.28)};");
                escuro.Append($"--sobre-marca:{e.TextoLegivel};");
                // Repostos mesmo sem mudar de ideia sobre eles: o bloco
                // claro acima tem especificidade maior que o
                // [data-tema="escuro"] do app.css, entao o que ele define
                // e o escuro nao redefine vaza para o tema escuro.
                escuro.Append($"--sidebar-1:{e.Mover(-31, 0).Hex};");
                escuro.Append($"--sidebar-2:{e.Mover(-25, 0).Hex};");
                escuro.Append($"--sidebar-3:{e.Mover(-19, 0).Hex};");
            }

            if (s != null)
            {
                var b = Cor3.De(s.Hex);

                // Tudo que fica em cima da cor secundaria sai dela: o que
                // escreve, o segundo tom e o veu dos hovers e separadores.
                // E por isto que uma secundaria clara nao quebra o menu --
                // o branco vira preto aqui, de uma vez, para a lateral
                // inteira e para os topos de carvao do sistema.
                var porCima = Cor3.De(b.TextoLegivel);
                var claraPorBaixo = b.TextoLegivel != "#ffffff";

                var sobre = porCima.Hex;
                var sobre2 = porCima.Mover(claraPorBaixo ? +12 : -9, 0).Hex;
                var veu = claraPorBaixo ? "0, 0, 0" : "255, 255, 255";

                // O texto do menu nao e o branco cheio: e ele puxado para
                // perto do fundo, para o nome do item nao gritar mais que
                // o item aceso. Mesma ideia do #c3c8d0 sobre o carvao.
                var texto = claraPorBaixo ? "#3b4350" : "#c3c8d0";
                var fraco = claraPorBaixo ? "#6a7280" : "#7d8491";

                foreach (var bloco in new[] { claro, escuro })
                {
                    bloco.Append($"--carvao:{b.Hex};");
                    bloco.Append($"--sobre-carvao:{sobre};");
                    bloco.Append($"--sobre-carvao-2:{sobre2};");
                    bloco.Append($"--veu-carvao:{veu};");
                    bloco.Append($"--lateral-texto:{texto};");
                    bloco.Append($"--lateral-fraco:{fraco};");
                }

                claro.Append($"--lateral-fundo:{b.Hex};");
                // No escuro a lateral fica um tom abaixo da pagina para se
                // separar dela, como o #0f1216 original faz.
                escuro.Append($"--lateral-fundo:{b.Mover(-4, 0).Hex};");

                // As letras da marca no login so acompanham a secundaria
                // se derem para ler sobre o fundo da pagina. Uma
                // secundaria clara escrita sobre o #f7f8fa do tema claro
                // sumiria -- ali o preto original continua valendo.
                if (b.Contraste(FundoClaro) >= 3)
                    claro.Append($"--login-letra:{b.Hex};");

                // Branco, e nao a cor secundaria: no tema escuro a marca
                // aparece sobre o fundo escuro, e pinta-la de um tom
                // escuro a apagaria. Precisa estar escrito -- sem isto o
                // bloco claro acima vence por especificidade e a marca
                // some no escuro.
                escuro.Append("--login-letra:#ffffff;");
            }

            var css = new StringBuilder();
            css.Append("html:root{").Append(claro).Append('}');
            css.Append("html:root[data-tema=\"escuro\"]{").Append(escuro).Append('}');
            return css.ToString();
        }

        /// <summary>
        /// Uma cor em HSL, para derivar os tons vizinhos. Os passos sao em
        /// pontos percentuais, nao em fator: somar 10 a uma luminosidade
        /// de 52% da 62%, previsivel, enquanto multiplicar por 1,2 daria
        /// saltos diferentes conforme o tom de partida.
        /// </summary>
        private readonly record struct Cor3(double H, double S, double L)
        {
            public static Cor3 De(string hex)
            {
                var (r, g, b) = Bytes(hex);
                double rr = r / 255.0, gg = g / 255.0, bb = b / 255.0;

                var max = Math.Max(rr, Math.Max(gg, bb));
                var min = Math.Min(rr, Math.Min(gg, bb));
                var l = (max + min) / 2;
                double h = 0, s = 0;

                if (max - min > 0.0001)
                {
                    var d = max - min;
                    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

                    if (max == rr) h = (gg - bb) / d + (gg < bb ? 6 : 0);
                    else if (max == gg) h = (bb - rr) / d + 2;
                    else h = (rr - gg) / d + 4;

                    h *= 60;
                }

                return new Cor3(h, s * 100, l * 100);
            }

            /// <summary>
            /// Desloca luminosidade e saturacao, em pontos.
            ///
            /// Tom neutro nao recebe saturacao: num cinza o matiz e um
            /// residuo, e somar saturacao em cima dele inventa uma cor que
            /// ninguem escolheu. Branco tem matiz 0, entao o passo que
            /// deixa o laranja mais vivo o deixava rosado; o preto do
            /// sistema puxa para o azul, e virava azul de verdade.
            /// </summary>
            private const double NeutroAbaixoDe = 20;

            public Cor3 Mover(double luz, double saturacao) =>
                new(H,
                    S < NeutroAbaixoDe ? S : Math.Clamp(S + saturacao, 0, 100),
                    Math.Clamp(L + luz, 0, 100));

            public string Hex
            {
                get
                {
                    var (r, g, b) = Rgb;
                    return $"#{r:x2}{g:x2}{b:x2}";
                }
            }

            public string Rgba(double alfa)
            {
                var (r, g, b) = Rgb;
                return $"rgba({r},{g},{b},{alfa.ToString(CultureInfo.InvariantCulture)})";
            }

            /// <summary>
            /// Preto ou branco por cima desta cor, o que tiver mais
            /// contraste. O laranja da marca pede preto: branco sobre ele
            /// da 3,2:1, abaixo do minimo legivel.
            /// </summary>
            public string TextoLegivel
            {
                get
                {
                    var contrasteComPreto = (Luz + 0.05) / 0.05;
                    var contrasteComBranco = 1.05 / (Luz + 0.05);
                    return contrasteComPreto >= contrasteComBranco ? "#14171c" : "#ffffff";
                }
            }

            /// <summary>Razao de contraste entre duas cores, de 1 a 21.</summary>
            public double Contraste(Cor3 outra)
            {
                var a = Math.Max(Luz, outra.Luz);
                var b = Math.Min(Luz, outra.Luz);
                return (a + 0.05) / (b + 0.05);
            }

            /// <summary>
            /// A mesma cor, afastada do fundo ate dar para distinguir os
            /// dois. Mantem o tom e a saturacao: so a luminosidade anda.
            ///
            /// O minimo de 3 e o que a WCAG pede para a borda de um
            /// componente -- nao e texto que esta em jogo aqui, e o botao
            /// inteiro se separando do cartao atras dele.
            /// </summary>
            public Cor3 ComContraste(Cor3 fundo, double minimo)
            {
                if (Contraste(fundo) >= minimo) return this;

                // Fundo escuro pede clarear; fundo claro pede escurecer.
                var passo = fundo.Luz < 0.18 ? 2.0 : -2.0;
                var atual = this;

                for (var i = 0; i < 50; i++)
                {
                    var proxima = atual.Mover(passo, 0);

                    // Bateu no preto ou no branco e ainda nao deu: nao ha
                    // para onde ir, e devolver o limite e melhor do que
                    // girar a toa.
                    if (Math.Abs(proxima.L - atual.L) < 0.001) break;

                    atual = proxima;
                    if (atual.Contraste(fundo) >= minimo) break;
                }

                return atual;
            }

            /// <summary>Luminancia relativa, a mesma conta da WCAG.</summary>
            private double Luz
            {
                get
                {
                    var (r, g, b) = Rgb;
                    return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
                }
            }

            private (int R, int G, int B) Rgb
            {
                get
                {
                    double h = H / 360, s = S / 100, l = L / 100;
                    if (s < 0.0001)
                    {
                        var cinza = (int)Math.Round(l * 255);
                        return (cinza, cinza, cinza);
                    }

                    var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                    var p = 2 * l - q;

                    return (Canal(p, q, h + 1.0 / 3), Canal(p, q, h), Canal(p, q, h - 1.0 / 3));
                }
            }

            private static int Canal(double p, double q, double t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;

                double v;
                if (t < 1.0 / 6) v = p + (q - p) * 6 * t;
                else if (t < 1.0 / 2) v = q;
                else if (t < 2.0 / 3) v = p + (q - p) * (2.0 / 3 - t) * 6;
                else v = p;

                return (int)Math.Round(v * 255);
            }

            private static double Linear(int canal)
            {
                var c = canal / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            private static (int R, int G, int B) Bytes(string hex)
            {
                var limpo = hex.TrimStart('#');
                return (
                    int.Parse(limpo[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    int.Parse(limpo.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    int.Parse(limpo.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
        }
    }
}
