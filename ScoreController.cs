using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using GolfWorld1.Models;
using Microsoft.AspNet.Identity;
using PagedList;

namespace GolfWorld1.Controllers
{
    public class ScoreController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Score
        //public ActionResult Index()
        //{
        //    return View(db.Courses.ToList());
        //}

        public ActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.NameSortParam = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.AddressSortParam = sortOrder == "address" ? "address_desc" : "address";
            ViewBag.DateSortParam = sortOrder == "date" ? "date_desc" : "date";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            //ViewBag.CurrentSort = sortOrder;

            var courses = from c in db.Courses select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(s => s.Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name":
                    courses = courses.OrderBy(x => x.Name);
                    break;
                case "name_desc":
                    courses = courses.OrderByDescending(x => x.Name);
                    break;
                case "address":
                    courses = courses.OrderBy(x => x.Address);
                    break;
                case "address_desc":
                    courses = courses.OrderByDescending(x => x.Address);
                    break;
                case "city":
                    courses = courses.OrderBy(x => x.City);
                    break;
                case "city_desc":
                    courses = courses.OrderByDescending(x => x.City);
                    break;
                default:
                    courses = courses.OrderBy(x => x.Name);
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(courses.ToPagedList(pageNumber, pageSize));
        }

        // GET: Score/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ScoreCard scoreCard = db.ScoreCards.Find(id);
            if (scoreCard == null)
            {
                return HttpNotFound();
            }
            return View(scoreCard);
        }

        // GET: Score/Create
        public ActionResult Create()
        {
            List<ScoreCard> scoreCards = new List<ScoreCard>();
            scoreCards.Add(new ScoreCard());
            return View(scoreCards);
        }

        // POST: Score/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "SCrowID,CreateDate,UpdateDate,FRID,FUserID,FTCID,RowNumber,Theme,H01,H02,H03,H04,H05,H06,H07,H08,H09,H10,H11,H12,H13,H14,H15,H16,H17,H18,HOut,HIn,HTotal")] List<ScoreCard> scoreCards)
        {
            if (ModelState.IsValid)
            {
                foreach (ScoreCard score in scoreCards)
                {
                    //score.CreateDate = Convert.ToDateTime(score.CreateDate);
                    //score.UpdateDate = Convert.ToDateTime(score.UpdateDate);
                    db.ScoreCards.Add(score);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(scoreCards);
        }

        // GET: Course/CreateCourse
        public ActionResult SelectTee(int id)
        {
            Course course = db.Courses.Find(id);
            //SelectList tees = new SelectList();
            List<SelectListItem> tees = new List<SelectListItem>();

            foreach (var a in course.TCInfos)
            {
                if (!string.IsNullOrEmpty(a.Name))
                {
                    tees.Add(new SelectListItem(){Value=a.Name, Text=a.Name});
                }
            }

            ViewData["teeNames"] = tees;

            return View();
        }

        // POST: Course/CreateTees
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult SelectTee(int? id, string gender, string teeName, DateTime playStartDateTime)
        {
            // here, build up Round so it can have
            // Round.UserName, Round.PlayStartDate, Round.Course.CourseName, Round.Course.Rating/Slope
            // Round.ScoreCards... That's it!!!

            Course course = db.Courses.Find(id);

            // create top 3 rows for scorecard
            TeeCommonInfo tciPar =
                (from a in course.TCInfos where a.Gender == gender where a.Theme == "Par" select a).FirstOrDefault();

            TeeCommonInfo tciHandicap =
                (from a in course.TCInfos where a.Gender == gender where a.Theme == "Handicap" select a).FirstOrDefault();

            TeeCommonInfo tciDistance =
                (from a in course.TCInfos where a.Gender == gender where a.Theme == "Distance" where a.Name == teeName select a).FirstOrDefault();

            // create a new empty round so that we can have a roundID
            Round round = new Round() { RecordDate = DateTime.Now, PlayDate = playStartDateTime, PlayStartTime = playStartDateTime, PlayEndTime = playStartDateTime.AddHours(4.5), CourseID = course.GCID, TeeID = tciDistance.TCID };

            round.Course = course;

            string aaa = User.Identity.GetUserId();

            round.UserGUID = Guid.Parse(User.Identity.GetUserId());
            round.UserName = User.Identity.Name;

            // to get a roundID
            // save partial info to round, later once a user completed entering score, then fully save it
            db.Rounds.Add(round);
            db.SaveChanges();

            // ??? here ???
            // do I have to do this?
            course.Rounds.Add(round);

            // create all the score card rows
            ScoreCard sc0 = new ScoreCard()
                            {
                                CreateDate = DateTime.Today,
                                UpdateDate = DateTime.Today,
                                //FRID = round.RID,
                                FTCID = tciPar.TCID,
                                RowNumber = 0,
                                Theme = "Par"
                            };
            ScoreCard sc1 = new ScoreCard()
                            {
                                CreateDate = DateTime.Today,
                                UpdateDate = DateTime.Today,
                                //FRID = round.RID,
                                FTCID = tciHandicap.TCID,
                                RowNumber = 1,
                                Theme = "Handicap"
                            };
            ScoreCard sc2 = new ScoreCard()
                            {
                                CreateDate = DateTime.Today,
                                UpdateDate = DateTime.Today,
                                //FRID = round.RID,
                                FTCID = tciDistance.TCID,
                                RowNumber = 2,
                                Theme = "Distance"
                            };

            CopyPropertiesH(tciPar, sc0);
            CopyPropertiesH(tciHandicap, sc1);
            CopyPropertiesH(tciDistance, sc2);

            sc0.HOut = tciPar.HOut;
            sc0.HIn = tciPar.HIn;
            sc0.HTotal = tciPar.HTotal;
            sc1.HOut = tciHandicap.HOut;
            sc1.HIn = tciHandicap.HIn;
            sc1.HTotal = tciHandicap.HTotal;
            sc2.HOut = tciDistance.HOut;
            sc2.HIn = tciDistance.HIn;
            sc2.HTotal = tciDistance.HTotal;

            List<ScoreCard> scoreCards = new List<ScoreCard>();
            scoreCards.Add(sc0);
            scoreCards.Add(sc1);
            scoreCards.Add(sc2);

            scoreCards.Add(new ScoreCard() { RowNumber = 3, Theme = "Score" });
            scoreCards.Add(new ScoreCard() { RowNumber = 4, Theme = "AltScore" });
            scoreCards.Add(new ScoreCard() { RowNumber = 5, Theme = "Putt" });
            scoreCards.Add(new ScoreCard() { RowNumber = 6, Theme = "Fairway" });
            scoreCards.Add(new ScoreCard() { RowNumber = 7, Theme = "GIR" });
            scoreCards.Add(new ScoreCard() { RowNumber = 8, Theme = "Pitch" });
            scoreCards.Add(new ScoreCard() { RowNumber = 9, Theme = "Chip" });
            scoreCards.Add(new ScoreCard() { RowNumber = 10, Theme = "GSBunker" });
            scoreCards.Add(new ScoreCard() { RowNumber = 11, Theme = "Penalty" });

            // now add scoreCards to round.ScoreCards
            round.ScoreCards = scoreCards;

            TempData["Round"] = round;

            // pass only round

            if (ModelState.IsValid)
            {
                return RedirectToAction("EnterScoreCard", "Score");
            }

            return View();
        }

        // GET: Score/EnterScoreCard
        public ActionResult EnterScoreCard()
        {
            Round round = null;
            if (TempData["Round"] != null)
            {
                round = (Round)TempData["Round"];
            }

            List<SelectListItem> fairwayNumbers = new List<SelectListItem>();
            List<SelectListItem> girNumbers = new List<SelectListItem>();

            fairwayNumbers.Add(new SelectListItem() { Value = "0", Text = "Hit: 0", Selected = true });
            fairwayNumbers.Add(new SelectListItem() { Value = "1", Text = "Pull: 1", Selected = false });
            fairwayNumbers.Add(new SelectListItem() { Value = "2", Text = "Push: 2", Selected = false });
            fairwayNumbers.Add(new SelectListItem() { Value = "3", Text = "Hook: 3", Selected = false });
            fairwayNumbers.Add(new SelectListItem() { Value = "4", Text = "Slice: 4", Selected = false });

            girNumbers.Add(new SelectListItem() { Value = "0", Text = "Yes: 0", Selected = true });
            girNumbers.Add(new SelectListItem() { Value = "1", Text = "No : 1", Selected = false });

            ViewData["fairwayNumbers"] = fairwayNumbers;
            ViewData["girNumbers"] = girNumbers;

            return View(round);
        }

        // POST: Score/EnterScoreCard
        [HttpPost]
        [Authorize]
        public ActionResult EnterScoreCard(Round round)
        {
            // we need to fill some columns of round for summary of the round
            round.Score = int.Parse(round.ScoreCards.Where(a => a.Theme == "Score").Select(b => b.HTotal).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Score").Select(b => b.HTotal).FirstOrDefault().ToString());
            round.Putt = int.Parse(round.ScoreCards.Where(a => a.Theme == "Putt").Select(b => b.HTotal).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Putt").Select(b => b.HTotal).FirstOrDefault().ToString());
            round.GIR = double.Parse(round.ScoreCards.Where(a => a.Theme == "GIR").Select(b => b.HTotal).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "GIR").Select(b => b.HTotal).FirstOrDefault().ToString());
            round.FH = double.Parse(round.ScoreCards.Where(a => a.Theme == "Fairway").Select(b => b.HTotal).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Fairway").Select(b => b.HTotal).FirstOrDefault().ToString());

            round.OutScore = int.Parse(round.ScoreCards.Where(a => a.Theme == "Score").Select(b => b.HOut).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Score").Select(b => b.HOut).FirstOrDefault().ToString());
            round.OutPutt = int.Parse(round.ScoreCards.Where(a => a.Theme == "Putt").Select(b => b.HOut).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Putt").Select(b => b.HOut).FirstOrDefault().ToString());
            round.OutGIR = double.Parse(round.ScoreCards.Where(a => a.Theme == "GIR").Select(b => b.HOut).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "GIR").Select(b => b.HOut).FirstOrDefault().ToString());
            round.OutFH = double.Parse(round.ScoreCards.Where(a => a.Theme == "Fairway").Select(b => b.HOut).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Fairway").Select(b => b.HOut).FirstOrDefault().ToString());

            round.InScore = int.Parse(round.ScoreCards.Where(a => a.Theme == "Score").Select(b => b.HIn).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Score").Select(b => b.HIn).FirstOrDefault().ToString());
            round.InPutt = int.Parse(round.ScoreCards.Where(a => a.Theme == "Putt").Select(b => b.HIn).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Putt").Select(b => b.HIn).FirstOrDefault().ToString());
            round.InGIR = double.Parse(round.ScoreCards.Where(a => a.Theme == "GIR").Select(b => b.HIn).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "GIR").Select(b => b.HIn).FirstOrDefault().ToString());
            round.InFH = double.Parse(round.ScoreCards.Where(a => a.Theme == "Fairway").Select(b => b.HIn).FirstOrDefault().ToString() == "" ? "0" : round.ScoreCards.Where(a => a.Theme == "Fairway").Select(b => b.HIn).FirstOrDefault().ToString());

            if (ModelState.IsValid)
            {
                // update Rounds set [values...] where RID = round.RID
                Round currentRound = db.Rounds.FirstOrDefault(r => r.RID == round.RID);
                if (currentRound != null)
                {
                    db.Entry(currentRound).CurrentValues.SetValues(round);
                }
            }

            // insert into ScoreCards values ...
            foreach (ScoreCard score in round.ScoreCards)
            {
                score.CreateDate = DateTime.Now;  // these two values are missing... why
                score.UpdateDate = DateTime.Now;
                score.FRID = round.RID;
                db.ScoreCards.Add(score);
            }

            //db.Rounds.AddOrUpdate(round);

            db.SaveChanges();
            return RedirectToAction("Index", "Round");
            return View();
        }

        // a: source b: destination (copy properties in a to b)
        public void CopyPropertiesH(object a, object b)
        {
            Type typeB = b.GetType();

            foreach (PropertyInfo property in a.GetType().GetProperties())
            {
                if (!property.CanRead || (property.GetIndexParameters().Length > 0))
                    continue;

                PropertyInfo other = typeB.GetProperty(property.Name);
                if ((other != null) && (other.CanWrite) && other.Name.StartsWith("H") && Char.IsNumber(other.Name[1]))
                {
                    other.SetValue(b, property.GetValue(a, null), null);
                }
            }
        }

        [HttpPost]
        public ActionResult SelectCourse(List<ScoreCard> scoreCards)
        {
            foreach (ScoreCard score in scoreCards)
            {
                db.ScoreCards.Add(score);
            }

            db.SaveChanges();
            return RedirectToAction("Index");
            return View();
        }

        // GET: Score/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ScoreCard scoreCard = db.ScoreCards.Find(id);
            if (scoreCard == null)
            {
                return HttpNotFound();
            }
            return View(scoreCard);
        }

        // POST: Score/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "SCrowID,CreateDate,UpdateDate,FRID,FUserID,FTCID,RowNumber,Theme,H01,H02,H03,H04,H05,H06,H07,H08,H09,H10,H11,H12,H13,H14,H15,H16,H17,H18,HOut,HIn,HTotal")] ScoreCard scoreCard)
        {
            if (ModelState.IsValid)
            {
                db.Entry(scoreCard).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(scoreCard);
        }

        // GET: Score/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ScoreCard scoreCard = db.ScoreCards.Find(id);
            if (scoreCard == null)
            {
                return HttpNotFound();
            }
            return View(scoreCard);
        }

        // POST: Score/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ScoreCard scoreCard = db.ScoreCards.Find(id);
            db.ScoreCards.Remove(scoreCard);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
