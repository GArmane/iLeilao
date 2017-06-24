using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace server.Controllers
{
    [Produces("application/json")]
    [Route("api/Solicitacoes")]
    //[Authorize]
    public class SolicitacoesController : Controller
    {
        private readonly LeilaoContext context;

        public SolicitacoesController(LeilaoContext ctx)
        {
            context = ctx;
        }

        // GET: api/Solicitacoes
        //Retorna todas as solicita��es
        [HttpGet]
        public IActionResult GetAll()
        {
            var solicitacoes = context.Solicitacao
                .Include(s => s.Lote)
                .Include(s => s.Lote.Produtos);

            return Ok(solicitacoes);
        }

        //Pega todas as solicita��es do usu�rio
        // GET: api/Solicitacoes/usuario/usuarioId
        [HttpGet("usuario/{usuarioId}")]
        public IActionResult GetUsuario([FromRoute] string usuarioId)
        {
            var solicitacoes = context.Solicitacao
                .Include(s => s.Lote)
                .Include(s => s.Lote.Produtos)
                .Where(s => s.UsuarioId.Equals(usuarioId));
            return Ok(solicitacoes);
        }

        //Pega todas solicita��es pendentes
        // GET: api/pendentes
        [HttpGet("pendentes")]
        public IActionResult GetPendentes()
        {
            var solicitacoes = context.Solicitacao
                .Include(s => s.Lote)
                .Include(s => s.Lote.Produtos)
                .Where(s => s.Status.Equals(StatusSolicitacao.EmAguardo));
            return Ok(solicitacoes);
        }

        //Pega uma solicita��o espec�fica
        // GET: api/Solicitacoes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var solicitacao = await context.Solicitacao
                .Include(s => s.Lote)
                .Include(s => s.Lote.Produtos)
                .FirstOrDefaultAsync(s => s.Id.Equals(id));

            if (solicitacao != null)
            {
                return Ok(solicitacao);
            }
            else
            {
                ModelState.AddModelError("Solicita��o", "Solicita��o n�o encontrada.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        // PUT: api/Solicitacoes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] int id, [FromBody] Solicitacao novaSolicitacao)
        {
            var solicitacao = await context.Solicitacao.AsNoTracking().FirstOrDefaultAsync(s => s.Id.Equals(novaSolicitacao.Id));

            if (solicitacao != null)
            {
                solicitacao = novaSolicitacao;

                context.Solicitacao.Update(solicitacao);
                await context.SaveChangesAsync();

                return Ok(solicitacao);
            }
            else
            {
                ModelState.AddModelError("Solicita��o", "Solicita��o n�o encontrada.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }

        }

        // POST: api/Solicitacoes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Solicitacao solicitacao)
        {

            var usuarioExists = await context.Users.AnyAsync(u => u.Id.Equals(solicitacao.UsuarioId));

            if (usuarioExists)
            {
                context.Solicitacao.Add(solicitacao);
                await context.SaveChangesAsync();

                return CreatedAtAction("Create", solicitacao);
            }
            else
            {
                ModelState.AddModelError("Usuario", "Usu�rio n�o cadastrado no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }


        // DELETE: api/Solicitacoes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var solicitacao = await context.Solicitacao
                .Include(s => s.Lote)
                .FirstOrDefaultAsync(s => s.Id.Equals(id));

            if (solicitacao != null)
            {
                //Nem Lote nem Produto dependem de Solicita��o, ent�o se deletar a solicita��o, 
                //produtos e lote se mant�m
                //mas ao apagar o lote (que � rela��o tanto de solicita��o, quanto de produto, 
                //a solicita��o e os produtos tbm s�o apagados
                context.Lote.Remove(solicitacao.Lote);
                await context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                ModelState.AddModelError("Solicita��o", "Solicita��o n�o existe no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        //POST: api/Solicitacoes/aprovar/idSolicitacao
        [HttpPost("aprovar/{id}")]
        public async Task<IActionResult> Aprovar([FromRoute] int id)
        {
            var solicitacao = await context.Solicitacao
                .Include(s => s.Lote)
                .Include(s => s.Lote.Produtos)
                .FirstOrDefaultAsync(s => s.Id.Equals(id));

            if (solicitacao != null)
            {
                solicitacao.Status = StatusSolicitacao.Aceito;

                context.Solicitacao.Update(solicitacao);
                await context.SaveChangesAsync();

                //CHAMAR O CONTROLE DE LEIL�O PARA A CRIA��O
                return Ok(solicitacao);
            }
            else
            {
                ModelState.AddModelError("Solicita��o", "Solicita��o n�o existe no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        //POST: api/Solicitacoes/reprovar/idSolicitacao
        [HttpPost("reprovar/{id}")]
        public async Task<IActionResult> Reprovar([FromRoute] int id)
        {
            var solicitacao = await context.Solicitacao
                .Include(s => s.Lote)
                .Include(s => s.Lote.Produtos)
                .FirstOrDefaultAsync(s => s.Id.Equals(id));

            if (solicitacao != null)
            {
                solicitacao.Status = StatusSolicitacao.Negado;

                context.Solicitacao.Update(solicitacao);
                await context.SaveChangesAsync();

                return Ok(solicitacao);
            }
            else
            {
                ModelState.AddModelError("Solicita��o", "Solicita��o n�o existe no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        [HttpPost("imagem/{id}")]
        public async Task<IActionResult> AddImagem([FromRoute] int id, [FromForm] IFormFile Imagem)
        {
            var produto = await context.Produto.FirstOrDefaultAsync(p => p.Id.Equals(id));

            if (produto != null)
            {
                //Abre a imagem como uma stream de dados
                var stream = Imagem.OpenReadStream();
                //Stream de memoria para qual a imagem ser� passada
                var ms = new MemoryStream();

                //passando o arquivo da imagem para stream de memoria
                await stream.CopyToAsync(ms);

                //passando o stream de mem�ria para byte[]
                var bytes = ms.ToArray();

                //passando o byte[] para string base64
                string img = Convert.ToBase64String(bytes);

                //formatando a string para funcionamento no navegador (uso de string interpolation)
                img = string.Format($"data:{Imagem.ContentType};base64,{img}");

                produto.Imagem = img;
                context.Produto.Update(produto);

                await context.SaveChangesAsync();

                //Retorno
                return Ok(produto);

            }
            else
            {
                ModelState.AddModelError("Produto", "Produto n�o existe no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }
    }
}