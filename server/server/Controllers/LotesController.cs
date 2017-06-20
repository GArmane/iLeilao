using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Controllers
{
    [Produces("application/json")]
    [Route("api/Lotes")]
    //[Authorize]
    public class LotesController : Controller
    {
        private readonly LeilaoContext ctx;

        public LotesController(LeilaoContext context)
        {
            ctx = context;
        }

        // GET: api/Lotes
        [HttpGet]
        public IEnumerable<Lote> GetAll()
        {
            return ctx.Lote.ToList();
        }

        // GET: api/Lotes/UsuarioId
        // Retorna todos lotes cadastrados do usu�rio especificado.
        [HttpGet("vendedor/{vendedorID}")]
        public IActionResult GetVendedorAll([FromRoute] int vendedorID)
        {
            // ERRO: Retorna uma lista vazia.
            // N�o sei se esse m�todo vai ser �til ou
            // pertence a essa classe.
            var vendedorLotes = ctx.Lote
                .Include(l => l.Vendedor)
                .Include(l => l.Produtos)
                .Where(l => l.VendedorId.Equals(vendedorID));
            return Ok(vendedorLotes);
        }

        // GET: api/Lotes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByID([FromRoute] int id)
        {
            // ERRO: se existe algum produto no lote, o servidor
            // n�o retorna resposta.
            // Entretanto, retorna informa��es do vendedor corretamente
            // caso n�o tenha produtos cadastrados.
            var lote = await ctx.Lote
                .Include(l => l.Vendedor)
                .Include(l => l.Produtos)
                .FirstOrDefaultAsync(l => l.Id.Equals(id));

            if(lote != null)
            {
                return Ok(lote);
            }
            else
            {
                ModelState.AddModelError("Usu�rio", "Lote n�o encontrado.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        // POST: api/Lotes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Lote lote)
        {
            var usuarioExists = ctx.Users.Any(u => u.Id.Equals(lote.VendedorId));

            if(usuarioExists)
            {
                ctx.Lote.Add(lote);
                await ctx.SaveChangesAsync();
                return CreatedAtAction("Create", lote);
            }
            else
            {
                ModelState.AddModelError("Usuario", "Usu�rio n�o cadastrado no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        // PUT: api/Lotes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit([FromRoute] int id, [FromBody] Lote novoLote)
        {
            var lote = await ctx.Lote.FirstOrDefaultAsync(l => l.Id.Equals(novoLote.Id));

            if(lote != null)
            {
                // Lote encontrado no banco de dados, proceder com altera��es
                lote.ValorMinimo = novoLote.ValorMinimo;
                lote.Vendedor = novoLote.Vendedor;

                // Salva altera��es no banco
                ctx.Lote.Update(lote);
                await ctx.SaveChangesAsync();
                return Ok(lote);
            }
            else
            {
                // Lote n�o encontrado, retornar erro
                ModelState.AddModelError("Lote", "Lote n�o encontrado no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        // DELETE: api/Lotes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var lote = await ctx.Lote.FirstOrDefaultAsync(l => l.Id.Equals(id));

            if(lote != null)
            {
                // Lote encontrado, proceder com exclus�o
                ctx.Lote.Remove(lote);
                await ctx.SaveChangesAsync();
                return Ok();
            }
            else
            {
                // Lote n�o encontrado, retornar erro
                ModelState.AddModelError("Lote", "Lote n�o encontrado no sistema.");
                return NotFound(ModelState.Values.SelectMany(e => e.Errors));
            }
        }

        // Utilitario
        private bool Exists(int id)
        {
            return ctx.Lote.Any(e => e.Id == id);
        }
    }
}