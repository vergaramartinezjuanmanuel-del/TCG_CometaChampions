import json
import re

with open("scratch/cards-data.js", "r", encoding="utf-8") as f:
    text = f.read()

no_comments = re.sub(r"//.*", "", text)
no_comments = re.sub(r"/\*[\s\S]*?\*/", "", no_comments)

m = re.search(r"CARDS_DATABASE\s*=\s*(\[[\s\S]*?\]);", no_comments)
raw_cards = m.group(1)

quoted = re.sub(r'([{,]\s*)([a-zA-Z0-9_]+)\s*:', r'\1"\2":', raw_cards)
quoted = re.sub(r',\s*([\]}])', r'\1', quoted)

cards = json.loads(quoted)
print("Successfully loaded cards:", len(cards))
categories = {}
for c in cards:
    cat = c.get("category", "unknown")
    categories[cat] = categories.get(cat, 0) + 1

print("Categories count:", categories)

# Check REVERSOS_INFO
m_rev = re.search(r"REVERSOS_INFO\s*=\s*(\[[\s\S]*?\]);", no_comments)
raw_rev = m_rev.group(1)
quoted_rev = re.sub(r'([{,]\s*)([a-zA-Z0-9_]+)\s*:', r'\1"\2":', raw_rev)
quoted_rev = re.sub(r',\s*([\]}])', r'\1', quoted_rev)
reversos = json.loads(quoted_rev)
print("Reversos:", reversos)

# Check CLANS_INFO
m_clans = re.search(r"CLANS_INFO\s*=\s*(\{[\s\S]*?\});", no_comments)
raw_clans = m_clans.group(1)
quoted_clans = re.sub(r'([{,]\s*)([a-zA-Z0-9_]+)\s*:', r'\1"\2":', raw_clans)
quoted_clans = re.sub(r',\s*([\]}])', r'\1', quoted_clans)
clans = json.loads(quoted_clans)
print("Clans:", list(clans.keys()))

with open("scratch/cards_db.json", "w", encoding="utf-8") as f:
    json.dump({"cards": cards, "reversos": reversos, "clans": clans}, f, indent=2, ensure_ascii=False)
print("Saved scratch/cards_db.json")
