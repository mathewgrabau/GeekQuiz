using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using TriviaSinglePage.Models;

namespace TriviaSinglePage.Controllers
{
    [Authorize]
    public class TriviaController : ApiController
    {
        private TriviaContext _db = new TriviaContext();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Retrieve the next question from the database for the specified user.
        /// </summary>
        /// <param name="userId">The user id for specified retrieval.</param>
        /// <returns></returns>
        private async Task<TriviaQuestion> NextQuestionAsync(string userId)
        {
            var lastQuestionId = await _db.TriviaAnswers
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.QuestionId)
                .Select(g => new { QuestionId = g.Key, Count = g.Count() })
                .OrderByDescending(q => new { q.Count, QuestionId = q.QuestionId })
                .Select(q => q.QuestionId)
                .FirstOrDefaultAsync();

            var questionsCount = await _db.TriviaQuestions.CountAsync();

            var nextQuestionId = (lastQuestionId % questionsCount) + 1;
            return await _db.TriviaQuestions.FindAsync(CancellationToken.None, nextQuestionId);
        }

        // Get api/Trivia
        /// <summary>
        /// Action that gets the next question for the authenticated user.
        /// </summary>
        /// <returns></returns>
        [ResponseType(typeof(TriviaQuestion))]
        public async Task<IHttpActionResult> Get()
        {
            var userId = User.Identity.Name;

            TriviaQuestion nextQuestion = await NextQuestionAsync(userId);

            if (nextQuestion == null)
            {
                return NotFound();
            }

            return Ok(nextQuestion);
        }

        /// <summary>
        /// Stores the specified answer in the database and returns a bool indicating whether or not the answer is correct.
        /// </summary>
        /// <param name="answer"></param>
        /// <returns>Whether the answer is correct.</returns>
        private async Task<bool> StoreAsync(TriviaAnswer answer)
        {
            _db.TriviaAnswers.Add(answer);

            await _db.SaveChangesAsync();
            var selectedOption = await _db.TriviaOptions.FirstOrDefaultAsync(o => o.Id == answer.OptionId && o.QuestionId == answer.QuestionId);

            return selectedOption.IsCorrect;
        }

        // POST api/Trivia
        /// <summary>
        /// Associates the answer to the authenticated user and stores the response.
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        [ResponseType(typeof(TriviaAnswer))]
        public async Task<IHttpActionResult> Post(TriviaAnswer answer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            answer.UserId = User.Identity.Name;

            var isCorrect = await StoreAsync(answer);
            return Ok<bool>(isCorrect);
        }
    }
}
