# [http://localhost:5080]
B=${1:-http://localhost:5080}

c() { 
  echo "--- $1 $2 $3"
  if [ -n "$3" ]; then
    curl -s -w ' [HTTP %{http_code}]\n' -X "$1" "$B$2" -H 'Content-Type: application/json' -d "$3"
  else
    curl -s -w ' [HTTP %{http_code}]\n' -X "$1" "$B$2"
  fi
}

c GET /api/types
echo "### сервер стартує без бази (потрібен свіжозапущений сервер)"
c GET /api/db
echo "### без бази: очікуємо 409 {\"error\":\"База не створена\"}"
c POST /api/tables '{"name":"X","columns":[{"name":"id","type":"integer"}]}'
c GET /api/tables/X
c DELETE /api/tables/X
c POST /api/tables/X/rows '{"values":["1"]}'
c PUT /api/tables/X/rows/0 '{"values":["1"]}'
c DELETE /api/tables/X/rows/0
c POST /api/operations/product '{"tableA":"A","tableB":"B","resultName":"R"}'
c POST /api/db/save '{"fileName":"nodb"}'
echo "### створення бази"
c POST /api/db '{"name":"Univ"}'
c GET /api/db
c POST /api/tables '{"name":"Students","columns":[{"name":"id","type":"integer"},{"name":"name","type":"string"},{"name":"span","type":"integerInvl"}]}'
c POST /api/tables '{"name":"Courses","columns":[{"name":"id","type":"integer"},{"name":"notes","type":"textFile"}]}'
echo "### помилки створення таблиці (очікуємо 400)"
c POST /api/tables '{"name":"Courses","columns":[{"name":"id","type":"integer"}]}'
c POST /api/tables '{"name":"Bad","columns":[{"name":"x","type":"blob"}]}'
echo "### рядки"
c POST /api/tables/Students/rows '{"values":["1","Alice","5..20"]}'
c POST /api/tables/Students/rows '{"values":["2","Bob","-3..0"]}'
c POST /api/tables/Students/rows '{"values":["3","Eve","7..7"]}'
c POST /api/tables/Students/rows '{"values":["x","Zed","20..5"]}'
c POST /api/tables/Courses/rows '{"values":["101","math.txt\nПривіт\nсвіт"]}'
c POST /api/tables/Courses/rows '{"values":["102","phys.txt\nformula"]}'
echo "### textFile: не-текст (U+FFFD, нульовий байт) має відхилятись (400)"
c POST /api/tables/Courses/rows '{"values":["103","pic.png\n\uFFFDPNG"]}'
c POST /api/tables/Courses/rows '{"values":["104","bin.dat\nab\u0000cd"]}'
c PUT /api/tables/Students/rows/1 '{"values":["2","Bobby","1..2"]}'
c DELETE /api/tables/Students/rows/2
c POST /api/tables/Students/rows '{"values":["3","Eve","7..7"]}'
c GET /api/tables/Nope
echo "### прямий добуток"
c POST /api/operations/product '{"tableA":"Students","tableB":"Students","resultName":"R"}'
c POST /api/operations/product '{"tableA":"Students","tableB":"Courses","resultName":"Enrolled"}'
curl -s "$B/api/tables/Enrolled" | python3 -c '
import sys, json
d = json.load(sys.stdin)
print("колонки:", [c["name"] for c in d["columns"]])
print("рядків у результаті:", len(d["rows"]), "(очікуємо 3 x 2 = 6)")
print("перший:", d["rows"][0])
print("останній:", d["rows"][-1])'
echo "### збереження / зчитування"
c POST /api/db/save '{"fileName":"demo"}'
c POST /api/db/save '{"fileName":"../evil"}'
c POST /api/db '{"name":"Empty"}'
c GET /api/db
c POST /api/db/load '{"fileName":"demo.json"}'
c GET /api/db
c POST /api/db/load '{"fileName":"missing.json"}'
echo "### зчитування старої бази з PNG у комірці: зрозуміла помилка (400), а не падіння"
mkdir -p "$(dirname "$0")/../TableDb.Web/data"
printf '%s' '{"Name":"Old","Tables":[{"Name":"Docs","Columns":[{"Name":"scan","TypeName":"textFile"}],"Rows":[["Screenshot.png\n\uFFFDPNG\u0000"]]}]}' \
  > "$(dirname "$0")/../TableDb.Web/data/old-png.json"
c POST /api/db/load '{"fileName":"old-png.json"}'
rm -f "$(dirname "$0")/../TableDb.Web/data/old-png.json"
echo "### некоректний JSON у тілі"
c POST /api/tables 'not json'
