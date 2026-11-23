// Trava a rolagem da pagina enquanto houver um modal aberto.
// Sem isto a barra de rolagem continua visivel ao lado do overlay e a tela
// fica com uma faixa que o fundo escurecido nao cobre.
(function () {
    function atualizar() {
        const aberto = !!document.querySelector('.modal-overlay');
        document.body.style.overflow = aberto ? 'hidden' : '';
    }

    function observar() {
        if (!document.body) return;
        new MutationObserver(atualizar).observe(document.body, {
            childList: true,
            subtree: true
        });
        atualizar();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', observar);
    } else {
        observar();
    }
})();

// Tema claro/escuro. O App.razor ja aplicou o tema salvo antes da primeira
// pintura; aqui ficam a leitura e a troca, chamadas pelo NavMenu.
window.tema = {
    ler: function () {
        try {
            var t = localStorage.getItem('tema');
            if (t === 'claro' || t === 'escuro') return t;
        } catch (e) { /* navegacao anonima ou storage bloqueado */ }

        // Sem escolha salva, segue a preferencia do sistema operacional.
        return matchMedia('(prefers-color-scheme: dark)').matches ? 'escuro' : 'claro';
    },

    aplicar: function (valor) {
        document.documentElement.setAttribute('data-tema', valor);
        try {
            localStorage.setItem('tema', valor);
        } catch (e) { /* a troca vale para esta sessao mesmo sem persistir */ }
    }
};

// Marca: logo e cores. As da barbearia o administrador escolhe e valem
// para todos -- vem no HTML do servidor (App.razor), na tag #cores-globais
// e no data-logo-global do <html>. Quem nao e administrador pode por as
// suas por cima, guardadas so no proprio navegador, como o tema.
//
// O CSS das cores vem pronto do servidor (Configs/Paleta.cs) e aqui so e
// guardado e colado numa tag <style>. Derivar os tons no JS tambem
// significaria manter a mesma formula em dois lugares.
window.marca = {
    // Devolve se ha logo, e nao a logo: o C# so precisa saber disso para
    // decidir se mostra o "voltar a logo do sistema", e a imagem em
    // base64 passa de meio megabyte -- atravessaria o circuito inteiro a
    // cada carga da pagina da conta sem servir para nada la.
    ler: function () {
        try {
            return {
                primaria: localStorage.getItem('marca.primaria') || 'padrao',
                secundaria: localStorage.getItem('marca.secundaria') || 'padrao',
                temLogo: !!localStorage.getItem('marca.logo')
            };
        } catch (e) {
            return { primaria: 'padrao', secundaria: 'padrao', temLogo: false };
        }
    },

    // Uso interno do reaplicar, que precisa da imagem de verdade.
    guardado: function (chave) {
        try {
            return localStorage.getItem('marca.' + chave) || '';
        } catch (e) {
            return '';
        }
    },

    aplicarCores: function (primaria, secundaria, css) {
        try {
            localStorage.setItem('marca.primaria', primaria);
            localStorage.setItem('marca.secundaria', secundaria);
            localStorage.setItem('marca.css', css || '');
        } catch (e) { /* vale para esta sessao mesmo sem persistir */ }

        window.marca.pintar(css || '');
    },

    aplicarLogo: function (dataUrl) {
        try {
            if (dataUrl) localStorage.setItem('marca.logo', dataUrl);
            else localStorage.removeItem('marca.logo');
        } catch (e) { /* idem */ }

        window.marca.pendurar(dataUrl || '');
    },

    // Administrador: as cores ja foram gravadas no banco. Troca a tag da
    // barbearia e tira as escolhas pessoais dele, que ficariam por cima.
    aplicarCoresGlobais: function (css) {
        try {
            localStorage.removeItem('marca.primaria');
            localStorage.removeItem('marca.secundaria');
            localStorage.removeItem('marca.css');
        } catch (e) { /* idem */ }

        window.marca.pintar('');

        var tag = document.getElementById('cores-globais');
        if (!css) {
            if (tag) tag.remove();
            return;
        }
        if (!tag) {
            tag = document.createElement('style');
            tag.id = 'cores-globais';
            document.head.appendChild(tag);
        }
        tag.textContent = css;
    },

    // Administrador: a logo ja foi gravada no banco; url vazia e sem logo.
    aplicarLogoGlobal: function (url) {
        try { localStorage.removeItem('marca.logo'); } catch (e) { /* idem */ }

        var raiz = document.documentElement;
        if (url) raiz.setAttribute('data-logo-global', url);
        else raiz.removeAttribute('data-logo-global');

        window.marca.pendurar(url || '');
    },

    // Logo da barbearia, para onde a tela volta sem logo pessoal.
    logoGlobal: function () {
        return document.documentElement.getAttribute('data-logo-global') || '';
    },

    // Cola o CSS numa tag unica no fim do <head>, depois do app.css.
    pintar: function (css) {
        var tag = document.getElementById('cores-proprias');

        if (!css) {
            if (tag) tag.remove();
            return;
        }

        if (!tag) {
            tag = document.createElement('style');
            tag.id = 'cores-proprias';
            document.head.appendChild(tag);
        }

        if (tag.textContent !== css) tag.textContent = css;
    },

    // A logo entra como variavel CSS, e nao mexendo no src de uma <img>:
    // assim o Blazor pode redesenhar a pagina a vontade sem desfazer nada,
    // e as tres variantes da marca pegam a troca de uma vez so.
    pendurar: function (dataUrl) {
        var raiz = document.documentElement;

        if (!dataUrl) dataUrl = window.marca.logoGlobal();

        if (!dataUrl) {
            raiz.style.removeProperty('--logo-propria');
            raiz.removeAttribute('data-logo');
            return;
        }

        raiz.style.setProperty('--logo-propria', 'url("' + dataUrl + '")');
        if (raiz.getAttribute('data-logo') !== 'propria') {
            raiz.setAttribute('data-logo', 'propria');
        }
    }
};

// A navegacao aprimorada do Blazor sincroniza os atributos do <html> com o
// HTML vindo do servidor, que nao tem data-tema: o atributo sumia e a tela
// voltava para o claro a cada troca de pagina. O script do <head> nao roda
// de novo nessa navegacao, entao o tema e reaplicado aqui -- e junto com
// ele a logo, que viaja no mesmo style do <html> e some pelo mesmo motivo.
(function () {
    const raiz = document.documentElement;

    function reaplicar() {
        const salvo = window.tema.ler();
        if (raiz.getAttribute('data-tema') !== salvo) {
            raiz.setAttribute('data-tema', salvo);
        }

        window.marca.pintar(window.marca.guardado('css'));

        // So repoe se mudou: pendurar() escreve no style do <html>, que e
        // justamente o que o observador abaixo vigia. A pessoal vence a da
        // barbearia, que vem do servidor a cada navegacao.
        const logo = window.marca.guardado('logo') || window.marca.logoGlobal();
        if (logo && raiz.style.getPropertyValue('--logo-propria') !== 'url("' + logo + '")') {
            window.marca.pendurar(logo);
        }
    }

    new MutationObserver(reaplicar).observe(raiz, {
        attributes: true,
        attributeFilter: ['data-tema', 'data-logo', 'data-logo-global', 'style']
    });

    document.addEventListener('enhancedload', reaplicar);
    reaplicar();
})();

// Atalhos globais: F2 abre a nova venda e F3 o novo agendamento, de qualquer
// tela. Vale tambem digitando num campo: tecla de funcao nao escreve nada.
document.addEventListener('keydown', function (evento) {
    if (evento.ctrlKey || evento.altKey || evento.metaKey || evento.shiftKey) return;

    const destino = { F2: '/venda/nova', F3: '/agendamento/novo' }[evento.key];
    if (!destino) return;

    // Na tela de login nao ha sistema para navegar.
    if (location.pathname.toLowerCase().startsWith('/login')) return;

    evento.preventDefault(); // o F3 do navegador abre a busca na pagina

    if (location.pathname === destino) return;

    if (window.Blazor && typeof window.Blazor.navigateTo === 'function') {
        window.Blazor.navigateTo(destino);
    } else {
        location.href = destino;
    }
});

// Olhinho de mostrar senha. Ouvinte delegado no documento em vez de um
// onclick no botao: a tela de login e renderizacao estatica e a navegacao
// aprimorada do Blazor troca o DOM, entao um ouvinte preso ao elemento
// se perderia na primeira navegacao.
document.addEventListener('click', function (evento) {
    const botao = evento.target.closest('[data-ver-senha]');
    if (!botao) return;

    const campo = document.getElementById(botao.dataset.verSenha);
    if (!campo) return;

    const mostrando = campo.type === 'text';
    campo.type = mostrando ? 'password' : 'text';

    botao.setAttribute('aria-pressed', String(!mostrando));
    botao.setAttribute('aria-label', mostrando ? 'Mostrar senha' : 'Ocultar senha');
    botao.classList.toggle('revelado', !mostrando);

    // O clique tira o foco do campo; devolver evita que a pessoa precise
    // clicar de novo para continuar digitando.
    campo.focus();
});

// Apoio do componente Seletor.
//
// Duas coisas que o Blazor sozinho nao resolve: saber que o clique caiu
// fora do componente, e saber se o painel cabe abaixo do gatilho.
window.seletor = {
    // Fecha ao clicar fora. Devolve um id para o componente cancelar o
    // registro quando for descartado, senao o ouvinte vaza a cada
    // dropdown aberto.
    registrarCliqueFora: function (elemento, referenciaDotNet) {
        const aoClicar = function (evento) {
            if (elemento && !elemento.contains(evento.target)) {
                referenciaDotNet.invokeMethodAsync('FecharPorCliqueFora');
            }
        };

        document.addEventListener('mousedown', aoClicar, true);

        const id = 'sel-' + Math.random().toString(36).slice(2);
        this._registros = this._registros || {};
        this._registros[id] = aoClicar;
        return id;
    },

    cancelarCliqueFora: function (id) {
        if (!this._registros || !this._registros[id]) return;
        document.removeEventListener('mousedown', this._registros[id], true);
        delete this._registros[id];
    },

    // true quando falta espaco abaixo e sobra acima: o painel abre para cima.
    abrirParaCima: function (elemento, alturaPainel) {
        if (!elemento) return false;
        const caixa = elemento.getBoundingClientRect();
        const abaixo = window.innerHeight - caixa.bottom;
        return abaixo < alturaPainel && caixa.top > abaixo;
    },

    // Mantem a opcao sob o cursor do teclado visivel na rolagem.
    rolarAteOpcao: function (painel, indice) {
        if (!painel) return;
        const opcao = painel.querySelectorAll('[role="option"]')[indice];
        if (opcao) opcao.scrollIntoView({ block: 'nearest' });
    },

    focar: function (elemento) {
        if (elemento) elemento.focus();
    }
};

// Envio do login: a tela e renderizacao estatica, entao nao ha circuito
// Blazor para desabilitar o botao. Feito aqui, no submit do formulario.
document.addEventListener('submit', function (evento) {
    const botao = evento.target.querySelector('button[type="submit"][data-texto-enviando]');
    if (!botao) return;

    botao.disabled = true;
    botao.textContent = botao.dataset.textoEnviando;
});

// Mascara de digitacao: <input data-mascara="telefone|cpf|cnpj|ctps|cep|moeda">.
// Delegado no document porque o Blazor cria e recria os campos o tempo
// todo. So formata enquanto a pessoa digita; o @bind do Blazor le o valor
// no change (ao sair do campo), ja formatado. Quem confere se o valor vale
// e o servidor (Configs/Documentos.cs).
(function () {
    function digitos(texto, max) {
        return texto.replace(/\D/g, '').slice(0, max);
    }

    const mascaras = {
        telefone: function (v) {
            const d = digitos(v, 11);
            if (d.length <= 2) return d.length ? '(' + d : '';
            const meio = d.length === 11 ? 7 : 6;
            if (d.length <= meio) return '(' + d.slice(0, 2) + ') ' + d.slice(2);
            return '(' + d.slice(0, 2) + ') ' + d.slice(2, meio) + '-' + d.slice(meio);
        },
        cpf: function (v) {
            const d = digitos(v, 11);
            return d.replace(/^(\d{3})(\d)/, '$1.$2')
                    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
                    .replace(/\.(\d{3})(\d)/, '.$1-$2');
        },
        cnpj: function (v) {
            const d = digitos(v, 14);
            return d.replace(/^(\d{2})(\d)/, '$1.$2')
                    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
                    .replace(/\.(\d{3})(\d)/, '.$1/$2')
                    .replace(/(\d{4})(\d)/, '$1-$2');
        },
        // Ate 11 digitos e CPF; passou disso, CNPJ.
        cpfcnpj: function (v) {
            return v.replace(/\D/g, '').length <= 11 ? mascaras.cpf(v) : mascaras.cnpj(v);
        },
        // CTPS fisica: numero (7) / serie (4).
        ctps: function (v) {
            const d = digitos(v, 11);
            return d.length > 7 ? d.slice(0, 7) + '/' + d.slice(7) : d;
        },
        cep: function (v) {
            const d = digitos(v, 8);
            return d.length > 5 ? d.slice(0, 5) + '-' + d.slice(5) : d;
        },
        // Centavos a partir dos digitos: "1234" vira "12,34".
        moeda: function (v) {
            const d = digitos(v, 12).replace(/^0+(?=\d)/, '');
            if (!d) return '';
            const inteiro = (d.length > 2 ? d.slice(0, -2) : '0').replace(/\B(?=(\d{3})+(?!\d))/g, '.');
            return inteiro + ',' + d.padStart(2, '0').slice(-2);
        }
    };

    document.addEventListener('input', function (evento) {
        const campo = evento.target;
        if (!(campo instanceof HTMLInputElement)) return;

        const mascara = mascaras[campo.dataset.mascara];
        if (!mascara) return;

        // Cursor no fim so quando ele ja estava no fim: editar no meio do
        // numero nao pode jogar o cursor para longe.
        const noFim = campo.selectionStart === campo.value.length;
        const novo = mascara(campo.value);
        if (novo === campo.value) return;

        campo.value = novo;
        if (noFim) campo.setSelectionRange(novo.length, novo.length);
    });
})();
