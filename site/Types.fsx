open System

type Sendung =
    { Titel: string
      Untertitel: string option
      Beschreibung: string option
      Beginn: DateTimeOffset
      Ende: DateTimeOffset
      Url: Uri option }

type Channel =
    { name: string
      logoUrl: string option }
    with override self.ToString() = self.name
