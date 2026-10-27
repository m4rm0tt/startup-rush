"""Compile le portage C# (tools/csharp/StartupRushReference, aucune dependance) et compare ses rendus des
10 pieces aux rendus Python, pixel par pixel. Resultat : build_cache/qa_csharp.json.
Ignore proprement si aucun SDK .NET n'est installe."""
import json, os, shutil, subprocess, sys, glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def main():
    out = {'ran': False}
    dotnet = shutil.which('dotnet')
    if not dotnet:
        out['note'] = 'SDK .NET absent : controle non execute'
    else:
        env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
        proj = os.path.join(ROOT, 'tools', 'csharp', 'StartupRushReference')
        bin_dir = os.path.join(ROOT, 'build_cache', 'csharp_bin')
        b = subprocess.run([dotnet, 'build', '-c', 'Release', '-o', bin_dir], cwd=proj, env=env, capture_output=True, text=True)
        out['build_ok'] = b.returncode == 0
        exe = os.path.join(bin_dir, 'StartupRushReference.exe' if os.name == 'nt' else 'StartupRushReference')
        tmp = os.path.join(ROOT, 'build_cache', 'csharp_renders')
        os.makedirs(tmp, exist_ok=True)
        rooms = []
        for f in sorted(glob.glob(os.path.join(ROOT, 'rooms', 'room_*.json'))):
            rid = os.path.splitext(os.path.basename(f))[0]
            r = subprocess.run([exe, ROOT, rid, os.path.join(tmp, rid + '.png'), os.path.join(ROOT, 'previews', 'rooms', rid + '.png')],
                               capture_output=True, text=True, env=env)
            line = r.stdout.strip().splitlines()[-1] if r.stdout.strip() else r.stderr.strip()[:200]
            rooms.append({'room': rid, 'identical': r.returncode == 0, 'detail': line})
            print(rid, line)
        shutil.rmtree(tmp, ignore_errors=True)
        out.update({'ran': True, 'rooms': rooms, 'all_identical': all(x['identical'] for x in rooms),
                    'dotnet': subprocess.run([dotnet, '--version'], capture_output=True, text=True).stdout.strip()})
    json.dump(out, open(os.path.join(ROOT, 'build_cache', 'qa_csharp.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
    print('qa_csharp :', {k: v for k, v in out.items() if k != 'rooms'})


if __name__ == '__main__':
    main()
