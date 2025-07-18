import pandas as pd, json
data = json.load(open("LocData.json", encoding="utf-8"))
df = pd.DataFrame(data)
df.rename(columns={"Translated": "translated"}, inplace=True)
df[["key", "context", "translated"]].to_csv("my_data.csv", index=False)