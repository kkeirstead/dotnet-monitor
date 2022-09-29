const util = require("util");
const fs = require('fs');
const path = require('path')

const main = async () => {
    try {
        const jsExec = util.promisify(require("child_process").exec);

        console.log("Installing npm dependencies");
        const { stdout, stderr } = await jsExec("npm install @actions/core @actions/github");
        console.log("npm-install stderr:\n\n" + stderr);
        console.log("npm-install stdout:\n\n" + stdout);
        console.log("Finished installing npm dependencies");

        const core = require('@actions/core');
        
        const textToSearch = core.getInput('textToSearch', { required: true });
        const textToAdd = core.getInput('textToAdd', { required: true });
        const paths = core.getInput('paths', {required: false});

        const insertFileNameParameter = "{insertFileName}";

        if (paths === null || paths.trim() === "")
        {
            return;
        }

        for (const currPath of paths.split(' ')) {
            fs.readFile(currPath, (err, content) => {
                if (err) throw err;

                if (!content.includes(textToSearch))
                {
                    var updatedTextToAdd = textToAdd;
                    if (textToAdd.includes(insertFileNameParameter))
                    {
                        const parsedPath = path.parse(currPath);
                        const encodedURIWithoutExtension = encodeURIComponent(path.join(parsedPath.dir, parsedPath.name))
                        updatedTextToAdd = textToAdd.replace(insertFileNameParameter, encodedURIWithoutExtension);
                    }

                    var contentStr = updatedTextToAdd + "\n\n" + content.toString();

                    fs.writeFile(currPath, contentStr, (err) => {});
                }
            });
        }
    } catch (error) {
        core.setFailed(error.message);
    }
}

// Call the main function to run the action
main();
