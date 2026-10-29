#!/usr/bin/zsh
set -euo pipefail

#cke='./cookie'
cke='./cookie_0'

qe='{
  "n": 11,
  "delay" : 10000,
  "k": "v",
  "x": [1, 2, "3", "x"]
}'

ql='{
  "username": "admin",
  "passwd" : "forget"
}'

req() {
  [[ ! -f ${cke} ]] && touch ${cke}

  local a=(
    'login'
    'logout'
    now
    hailstone
    'env'
    'echo'
  )

  curl -vs -X POST \
    --cookie ${cke} \
    --cookie-jar ${cke} \
    -H 'content-type: application/json' \
    -H 'accept: application/json' \
    --data ${qe} \
    'http://172.16.1.11:902/'${a[1]} | jq
}

purge() {
  rm -rf bin obj
}

cln() {
  dotnet clean api.csproj -c Release -v:q > /dev/null
}

bld() {
  dotnet build api.csproj -c Release -v:q
}

xxx() {
  echo -e '\e[0;96m||||||||||||||||||||||||||||||||||||||||||||||||||\e[0m\n'
  dotnet run --project api.csproj -c Release
}

update() {
  dotnet restore api.csproj
}

typeset -A opts
for k in ${(k)functions}; do
  opts+=(["$k[1]"]=$k)
done

[[ -z ${@} ]] && \
  paste -d ' ' <(print -l '\-'${(j:\n-:k)opts}) <(print -l $opts) && exit

while getopts ${(j::k)opts} opt ${@}; do
  $opts[$opt] ${@}
done
shift $(($OPTIND -1))

exit
